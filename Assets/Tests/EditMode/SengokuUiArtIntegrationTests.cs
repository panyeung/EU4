using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Focused EditMode coverage for the five original generated UI assets under
    /// Assets/Resources/Generated (docs/ART_ASSET_WORKFLOW.md): the six-portrait
    /// clan atlas, the sixteen-icon atlas, the rainy-visitor event illustration,
    /// and the parchment/indigo surface tiles. Asserts the importer configuration,
    /// the runtime loaders, the navigator's page integration, and the M02 scene
    /// generator's bindings. No Play Mode behavior is claimed here.
    /// </summary>
    public sealed class SengokuUiArtIntegrationTests
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

        // ------------------------------------------------------------ importer setup

        [Test]
        public void PortraitAtlas_ExposesSixDeliberateArchetypeSprites()
        {
            Sprite[] sprites = SengokuUiArt.LoadAtlas(SengokuUiArt.PortraitsAtlasPath);
            Assert.That(sprites.Length, Is.EqualTo(6),
                "the clan atlas is configured as six deliberate portraits");
            string[] expected =
            {
                "portrait_master", "portrait_lady", "portrait_main",
                "portrait_elder", "portrait_youth", "portrait_girl"
            };
            foreach (string name in expected)
            {
                Sprite sprite = SengokuUiArt.FindSprite(SengokuUiArt.PortraitsAtlasPath, name);
                Assert.That(sprite, Is.Not.Null, "the atlas exposes " + name);
                Assert.That(sprite.rect.width, Is.EqualTo(512f), name + " cell width");
                Assert.That(sprite.rect.height, Is.EqualTo(512f), name + " cell height");
            }
            // The four roster keys resolve to four distinct faces; the atlas keeps
            // stable names so the importer metadata stays the single source.
            Sprite[] roster =
            {
                SengokuUiArt.Portrait("master"), SengokuUiArt.Portrait("elder"),
                SengokuUiArt.Portrait("youth"), SengokuUiArt.Portrait("main")
            };
            Assert.That(roster.All(sprite => sprite != null), Is.True);
            Assert.That(roster.Select(sprite => sprite.name).Distinct().Count(),
                Is.EqualTo(4), "no two roster keys share one face");
        }

        [Test]
        public void IconAtlas_ExposesSixteenIconsAndEveryPageIconResolves()
        {
            Sprite[] sprites = SengokuUiArt.LoadAtlas(SengokuUiArt.IconsAtlasPath);
            Assert.That(sprites.Length, Is.EqualTo(16),
                "the icon atlas is configured as sixteen icons");
            Sprite[] pages = ReferenceGalleryNavigator.PageIconIndices
                .Select(SengokuUiArt.Icon).ToArray();
            Assert.That(pages.Length,
                Is.EqualTo(ReferenceGalleryNavigator.PageCount));
            Assert.That(pages.All(sprite => sprite != null), Is.True,
                "every reference page has a generated navigation icon");
            Assert.That(pages.Select(sprite => sprite.name).Distinct().Count(),
                Is.EqualTo(pages.Length), "page icons are distinct");
            foreach (int index in new[]
            {
                SengokuUiArt.IconRest, SengokuUiArt.IconTraining,
                SengokuUiArt.IconWork, SengokuUiArt.IconHorseTravel
            })
                Assert.That(SengokuUiArt.Icon(index), Is.Not.Null,
                    "action icon " + index + " resolves for the daily-action cards");
        }

        [Test]
        public void SurfaceTilesAndEventIllustration_Load()
        {
            Assert.That(SengokuUiArt.Parchment, Is.Not.Null,
                "the generated parchment tile is available for light panels");
            Assert.That(SengokuUiArt.IndigoPattern, Is.Not.Null,
                "the generated indigo textile is available for dark rails");
            Assert.That(SengokuUiArt.EventIllustration, Is.Not.Null,
                "the generated rainy-visitor illustration is available for the event page");
            Assert.That(SengokuUiArt.Parchment.rect.width, Is.GreaterThan(0f));
            Assert.That(SengokuUiArt.IndigoPattern.rect.width, Is.GreaterThan(0f));
            Assert.That(SengokuUiArt.EventIllustration.rect.width, Is.GreaterThan(0f));
        }

        // ------------------------------------------------------------ surface helpers

        [Test]
        public void ApplyParchmentSurface_BindsTiledSpriteAndKeepsTheTint()
        {
            GameObject panel = Create("Panel", typeof(RectTransform), typeof(Image));
            Image image = panel.GetComponent<Image>();
            Color tint = SengokuUiTheme.ParchmentLight;
            image.color = tint;

            SengokuUiArt.ApplyParchmentSurface(image);

            Assert.That(image.sprite, Is.SameAs(SengokuUiArt.Parchment));
            Assert.That(image.type, Is.EqualTo(Image.Type.Tiled));
            Assert.That(image.color, Is.EqualTo(tint),
                "the parchment tile tints under the panel's existing colour");
        }

        [Test]
        public void ApplyIndigoPatternOverlay_IsStretchedNonInteractiveAndIdempotent()
        {
            GameObject rail = Create("Rail", typeof(RectTransform), typeof(Image));
            rail.GetComponent<Image>().color = SengokuUiTheme.IndigoRail;

            Image first = SengokuUiArt.ApplyIndigoPatternOverlay(rail.transform, "Pattern");
            Image second = SengokuUiArt.ApplyIndigoPatternOverlay(rail.transform, "Pattern");

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.SameAs(first),
                "refreshing the pattern reuses the overlay instead of stacking copies");
            Assert.That(CountChildren(rail.transform, "Pattern"), Is.EqualTo(1));
            RectTransform rect = (RectTransform)first.transform;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(first.raycastTarget, Is.False,
                "the textile overlay must never block rail interactions");
            Assert.That(first.sprite, Is.SameAs(SengokuUiArt.IndigoPattern));
        }

        // ------------------------------------------------------------ navigator pages

        [Test]
        public void ShowPage_BindsGeneratedNavIconsAndRailPattern()
        {
            if (SengokuUiArt.Icon(ReferenceGalleryNavigator.PageIconIndices[0]) == null)
                Assert.Ignore("the generated icon atlas is not imported yet");

            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);
            GameObject nav = Create("Navigation", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            nav.transform.SetParent(host.transform, false);
            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Button" + i);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);
            navigator.BindReferences(
                Array.Empty<ReferenceGalleryPageRoot>(), buttons,
                Create("CharacterPanel", typeof(RectTransform)),
                Create("FamilyPanel", typeof(RectTransform)),
                Create("HouseholdPanel", typeof(RectTransform)),
                Create("MapPanel", typeof(RectTransform)),
                CreateButton("Launcher"), nav, Create("Header"));

            navigator.ShowPage(ReferenceGalleryPage.Character);
            navigator.ShowPage(ReferenceGalleryPage.Family);

            Sprite pattern = SengokuUiArt.IndigoPattern;
            if (pattern != null)
            {
                Transform overlay = nav.transform.Find(
                    ReferenceGalleryNavigator.NavPatternChildName);
                Assert.That(overlay, Is.Not.Null,
                    "the shared navigation rail carries the indigo textile");
                Assert.That(CountChildren(nav.transform,
                    ReferenceGalleryNavigator.NavPatternChildName), Is.EqualTo(1),
                    "repeat page switches never stack a second overlay");
            }
            for (int i = 0; i < buttons.Length; i++)
            {
                Transform icon = buttons[i].transform.Find(
                    ReferenceGalleryNavigator.NavIconChildName);
                Assert.That(icon, Is.Not.Null, "page button " + i + " carries its icon");
                Assert.That(CountChildren(buttons[i].transform,
                    ReferenceGalleryNavigator.NavIconChildName), Is.EqualTo(1),
                    "repeat page switches never stack a second icon");
                Image image = icon.GetComponent<Image>();
                Assert.That(image.raycastTarget, Is.False,
                    "the atlas icon must not block the button");
                if (SengokuUiArt.Icon(ReferenceGalleryNavigator.PageIconIndices[i]) != null)
                    Assert.That(image.sprite,
                        Is.SameAs(SengokuUiArt.Icon(
                            ReferenceGalleryNavigator.PageIconIndices[i])),
                        "page button " + i + " shows its own atlas icon");
            }
        }

        [Test]
        public void ShowPage_Events_AttachesIllustrationAndHouseholdHidesIt()
        {
            if (SengokuUiArt.EventIllustration == null)
                Assert.Ignore("the generated event illustration is not imported yet");

            GameObject host = Create("CanvasHost", typeof(RectTransform));
            ((RectTransform)host.transform).sizeDelta = new Vector2(1280f, 720f);
            ReferenceGalleryNavigator navigator = Create("Navigator", typeof(RectTransform))
                .AddComponent<ReferenceGalleryNavigator>();
            navigator.transform.SetParent(host.transform, false);

            GameObject eventRoot = Create("EventChoiceRoot", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            eventRoot.transform.SetParent(host.transform, false);
            GameObject modal = Create("EventChoiceModal", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            modal.transform.SetParent(eventRoot.transform, false);
            CreateTextChild("EventChoiceTitleText", modal.transform);
            CreateTextChild("EventChoiceBodyText", modal.transform);
            Create("EventChoiceOptions", typeof(RectTransform))
                .transform.SetParent(modal.transform, false);
            eventRoot.AddComponent<EventChoicePanel>();

            Button[] buttons = new Button[ReferenceGalleryNavigator.PageCount];
            for (int i = 0; i < buttons.Length; i++)
                buttons[i] = CreateButton("Button" + i);
            navigator.BindReferences(
                new[]
                {
                    new ReferenceGalleryPageRoot(eventRoot,
                        ReferenceGalleryPageMask.Household | ReferenceGalleryPageMask.Events)
                },
                buttons, Create("CharacterPanel", typeof(RectTransform)),
                Create("FamilyPanel", typeof(RectTransform)),
                Create("HouseholdPanel", typeof(RectTransform)),
                Create("MapPanel", typeof(RectTransform)),
                buttons[0], Create("Navigation", typeof(RectTransform), typeof(Image)),
                Create("Header"));

            navigator.ShowPage(ReferenceGalleryPage.Events);

            Transform illustration = FindDeep(modal.transform,
                ReferenceGalleryNavigator.EventIllustrationChildName);
            Assert.That(illustration, Is.Not.Null,
                "the events page card carries the generated story banner");
            Image banner = illustration.GetComponent<Image>();
            Assert.That(banner.sprite, Is.SameAs(SengokuUiArt.EventIllustration));
            Assert.That(banner.preserveAspect, Is.True);
            Assert.That(banner.raycastTarget, Is.False,
                "the illustration must never block the live choice buttons");
            RectTransform body = (RectTransform)FindDeep(modal.transform,
                "EventChoiceBodyText");
            Assert.That(body.offsetMax.y,
                Is.LessThanOrEqualTo(-(54f +
                    ReferenceGalleryNavigator.EventIllustrationHeight) + 0.01f),
                "the live story text starts below the banner, never under it");

            navigator.ShowPage(ReferenceGalleryPage.Household);
            Assert.That(illustration.gameObject.activeSelf, Is.False,
                "the compact household decision card keeps the banner hidden");
        }

        // ------------------------------------------------------------ scene generator

        [Test]
        public void Generator_BindsGeneratedArtIntoTheScene()
        {
            if (SengokuUiArt.Parchment == null || SengokuUiArt.IndigoPattern == null ||
                SengokuUiArt.Icon(SengokuUiArt.IconCrest) == null)
                Assert.Ignore("the generated UI atlases are not imported yet");

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject canvas = CreateIn(scene, "Canvas", null, typeof(Canvas));
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1280f, 720f);
                GameSession session = CreateIn(scene, "GameSession", null,
                    typeof(GameSession)).GetComponent<GameSession>();
                session.InitNewGame();
                CreateIn(scene, "WorldClockPanel", null, typeof(WorldClockPanel));
                GameObject familyRoot = CreateIn(scene, "FamilyRosterRoot", null);
                CreateIn(scene, "FamilyRosterPanel", familyRoot.transform);
                CreateIn(scene, "SuccessionRoot", null);
                CreateIn(scene, "FamilyChronicleRoot", null);
                CreateIn(scene, "FamilyDoctrineRoot", null);
                CreateIn(scene, "SC022FamilyFormationRoot", null);
                GameObject industryRoot = CreateIn(scene, "IndustryRoot", null);
                CreateIn(scene, "IndustryPanel", industryRoot.transform);
                CreateIn(scene, "M2WorldMapRoot", null);
                GameObject eventRoot = CreateIn(scene, "EventChoiceRoot", null);
                CreateIn(scene, "EventChoiceModal", eventRoot.transform);
                CreateIn(scene, "M03NpcAiInspectorRoot", null);

                Assert.That(AddM02DailyActionPanel.Update(scene),
                    Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));

                // Parchment tile on the two light columns.
                Assert.That(FindIn(scene, AddM02DailyActionPanel.ProfilePanelName)
                        .GetComponent<Image>().sprite,
                    Is.SameAs(SengokuUiArt.Parchment));
                Assert.That(FindIn(scene, AddM02DailyActionPanel.ActionPanelName)
                        .GetComponent<Image>().sprite,
                    Is.SameAs(SengokuUiArt.Parchment));
                // Indigo textile on the dark chrome, non-interactive.
                Transform topPattern = FindIn(scene, AddM02DailyActionPanel.TopBarName)
                    .Find("ReferenceTopBarPattern");
                Assert.That(topPattern, Is.Not.Null);
                Assert.That(topPattern.GetComponent<Image>().sprite,
                    Is.SameAs(SengokuUiArt.IndigoPattern));
                Assert.That(topPattern.GetComponent<Image>().raycastTarget, Is.False);
                Transform railPattern = FindIn(scene, AddM02DailyActionPanel.NavRailName)
                    .Find(ReferenceGalleryNavigator.NavPatternChildName);
                Assert.That(railPattern, Is.Not.Null);
                // One atlas icon per page button, in page order.
                string[] navNames =
                {
                    AddM02DailyActionPanel.NavHouseholdButtonName,
                    AddM02DailyActionPanel.NavCharacterButtonName,
                    AddM02DailyActionPanel.NavFamilyButtonName,
                    AddM02DailyActionPanel.NavMapButtonName,
                    AddM02DailyActionPanel.NavEventsButtonName
                };
                for (int i = 0; i < navNames.Length; i++)
                {
                    Transform icon = FindIn(scene, navNames[i])
                        .Find(ReferenceGalleryNavigator.NavIconChildName);
                    Assert.That(icon, Is.Not.Null, navNames[i] + " must carry its icon");
                    Assert.That(icon.GetComponent<Image>().sprite,
                        Is.SameAs(SengokuUiArt.Icon(
                            ReferenceGalleryNavigator.PageIconIndices[i])));
                    Assert.That(icon.GetComponent<Image>().raycastTarget, Is.False);
                }
                // Action cards show their atlas icons inside the icon tiles.
                Assert.That(FindIn(scene, AddM02DailyActionPanel.RestButtonName + "Icon")
                        .GetComponent<Image>().sprite,
                    Is.SameAs(SengokuUiArt.Icon(SengokuUiArt.IconRest)));
                Assert.That(FindIn(scene, AddM02DailyActionPanel.TravelCardName + "Icon")
                        .GetComponent<Image>().sprite,
                    Is.SameAs(SengokuUiArt.Icon(SengokuUiArt.IconHorseTravel)));
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(previous));
            }
        }

        // ------------------------------------------------------------ helpers

        private int CountChildren(Transform parent, string name)
        {
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name)
                    count++;
            return count;
        }

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

        private GameObject Create(string name, params Type[] components)
        {
            var go = components.Length == 0
                ? new GameObject(name, typeof(RectTransform))
                : new GameObject(name, components);
            _objects.Add(go);
            return go;
        }

        private static GameObject CreateIn(Scene scene, string name, Transform parent,
            params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (Type component in components)
                go.AddComponent(component);
            return go;
        }

        private static Transform FindIn(Scene scene, string name) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Single(transform => transform.name == name);

        private Button CreateButton(string name)
        {
            GameObject go = Create(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            Button button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
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
    }
}
