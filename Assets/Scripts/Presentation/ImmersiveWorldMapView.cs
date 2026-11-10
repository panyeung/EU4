using UnityEngine;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>Presentation-only map layout; existing controls retain their commands and references.</summary>
    [DisallowMultipleComponent]
    public sealed class ImmersiveWorldMapView : MonoBehaviour
    {
        private static readonly string[] TravelFields =
            { "DestinationNameText", "TravelTimeText", "ArrivalDateText" };
        private static readonly string[] InformationFields =
            { "CurrentDateText", "LocationMarkerText" };
        private bool travelExpanded = true;
        private bool informationExpanded = true;
        private bool configured;
        private Vector2 previousSize;
        private ReferenceGalleryNavigator navigator;

        public void BindNavigation(ReferenceGalleryNavigator owner) { navigator = owner; }

        public void Configure()
        {
            var root = (RectTransform)transform;
            bool resized = !configured || previousSize != root.rect.size;
            if (resized && root.rect.width < 950f) informationExpanded = false;
            if (resized && root.rect.width < 680f) travelExpanded = false;
            configured = true;
            previousSize = root.rect.size;
            Layout();
            ApplyVisibility();
        }

        public void SetTravelExpanded(bool expanded)
        {
            travelExpanded = expanded;
            Layout();
            ApplyVisibility();
        }

        public void SetInformationExpanded(bool expanded)
        {
            informationExpanded = expanded;
            Layout();
            ApplyVisibility();
        }

        private void LateUpdate()
        {
            if (!configured) return;
            Vector2 size = ((RectTransform)transform).rect.size;
            if (size != previousSize)
            {
                if (size.x < 950f) informationExpanded = false;
                if (size.x < 680f) travelExpanded = false;
                previousSize = size;
                Layout();
            }
            // Loading/returning restores the controller's original fields, even outside town.
            ApplyVisibility();
        }

        private void Layout()
        {
            Vector2 size = ((RectTransform)transform).rect.size;
            float left = -size.x / 2f, right = size.x / 2f;
            float top = size.y / 2f, bottom = -size.y / 2f;
            bool narrow = size.x < 740f;
            float dockHeight = narrow ? 192f : 172f;
            float width = Mathf.Min(224f, size.x * .23f);
            float lx = left + width / 2f + 8f, rx = right - width / 2f - 8f;
            float contentHeight = size.y - dockHeight - 104f;
            float contentY = (dockHeight - 104f) / 2f;
            Panel("MapTravelSurface", new Vector2(lx, contentY), new Vector2(width, contentHeight));
            Panel("MapInformationSurface", new Vector2(rx, contentY), new Vector2(width, contentHeight));
            Panel("MapActionSurface", new Vector2(0, bottom + dockHeight / 2f), new Vector2(size.x, dockHeight));
            Place("TitleText", new Vector2(0, top - 24f), new Vector2(240, 32));
            Toggle("MapTravelToggle", travelExpanded ? "收起出行计划" : "展开出行计划",
                new Vector2(lx, top - 67f), width, () => SetTravelExpanded(!travelExpanded));
            Toggle("MapInformationToggle", informationExpanded ? "收起当地信息" : "展开当地信息",
                new Vector2(rx, top - 67f), width, () => SetInformationExpanded(!informationExpanded));
            if (navigator != null)
            {
                Toggle("MapFamilyRoster", "家族名册", new Vector2(left + 78f, top - 24f), 140,
                    () => navigator.OpenMapFamilyRecord(false));
                Toggle("MapFamilyChronicle", "家族编年史", new Vector2(right - 88f, top - 24f), 160,
                    () => navigator.OpenMapFamilyRecord(true));
            }

            Place("DestinationNameText", new Vector2(lx, top - 132f), new Vector2(width - 20f, 60f));
            Place("TravelTimeText", new Vector2(lx, top - 210f), new Vector2(width - 20f, 54f));
            Place("ArrivalDateText", new Vector2(lx, top - 282f), new Vector2(width - 20f, 60f));
            Place("CurrentDateText", new Vector2(rx, top - 120f), new Vector2(width - 20f, 32f));
            Place("LocationMarkerText", new Vector2(rx, top - 164f), new Vector2(width - 20f, 40f));
            float feedbackWidth = narrow ? Mathf.Max(200f, size.x - 280f) : 390f;
            Place("FeedbackText", new Vector2(left + 12f + feedbackWidth / 2f, bottom + dockHeight - 40f),
                new Vector2(feedbackWidth, 64f));

            float mapLeft = left + (travelExpanded ? width + 24f : 16f);
            float mapRight = right - (informationExpanded ? width + 24f : 16f);
            float mapWidth = Mathf.Max(100f, mapRight - mapLeft);
            float center = (mapLeft + mapRight) / 2f;
            Image art = transform.Find(ReferenceGalleryNavigator.MapIllustrationChildName)?.GetComponent<Image>();
            if (art != null && art.sprite != null)
            {
                float fit = Mathf.Min(mapWidth / art.sprite.rect.width,
                    contentHeight / art.sprite.rect.height);
                Place(art.name, new Vector2(center, contentY), art.sprite.rect.size * fit);
                art.raycastTarget = false;
                art.transform.SetAsFirstSibling();
            }
            // Markers are independent controls, never baked into the illustration.
            Place("DestButton_0", new Vector2(center - mapWidth * .19f, 80), new Vector2(116, 48));
            Place("DestButton_1", new Vector2(center - mapWidth * .16f, -110), new Vector2(116, 48));
            Place("DestButton_2", new Vector2(center + mapWidth * .24f, 20), new Vector2(116, 48));
            float actionY = bottom + (narrow ? 100f : 34f);
            Place("ConfirmButton", new Vector2(left + 120f, actionY), new Vector2(216, 48));
            Place("CancelButton", new Vector2(left + 310f, actionY), new Vector2(140, 48));
            Place("M5SaveLoadRoot/M5SaveButton", new Vector2(-60, top - 65f), new Vector2(100, 38));
            Place("M5SaveLoadRoot/M5LoadButton", new Vector2(60, top - 65f), new Vector2(100, 38));
            Place("M5SaveLoadRoot/M5SaveStatusText", new Vector2(0, top - 100f), new Vector2(240, 24));
            Text saveStatus = transform.Find("M5SaveLoadRoot/M5SaveStatusText")?.GetComponent<Text>();
            if (saveStatus != null) saveStatus.color = SengokuUiTheme.Parchment;
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                if (text.transform.parent == transform)
                {
                    text.fontSize = text.name == "TitleText" ? 28 : 20;
                    text.color = SengokuUiTheme.Parchment;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.alignment = TextAnchor.MiddleLeft;
                    text.raycastTarget = false;
                }
            }
            Transform confirm = transform.Find("ConfirmButton");
            if (confirm != null && confirm.TryGetComponent(out Image confirmImage))
                confirmImage.color = SengokuUiTheme.Vermilion;
            if (confirm != null)
                foreach (Text text in confirm.GetComponentsInChildren<Text>(true))
                    text.color = SengokuUiTheme.ParchmentLight;
            Transform town = transform.Find("TownMenuRoot");
            if (town != null) town.SetAsLastSibling();
            LayoutClock(new Vector2(right - 145f, bottom + 40f));
        }

        private void LayoutClock(Vector2 position)
        {
            Transform clock = transform.parent != null ? transform.parent.Find("WorldClockPanel") : null;
            if (!(clock is RectTransform rect)) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = ((RectTransform)transform).anchoredPosition + position;
            rect.sizeDelta = new Vector2(280, 80);
            ClockRect(clock, "DateText", new Vector2(0, 30), new Vector2(260, 24), 18);
            ClockRect(clock, "AdvanceButton", new Vector2(-70, -6), new Vector2(132, 44), 20);
            ClockRect(clock, "MonthTurnPanel", Vector2.zero, Vector2.zero, 20);
            ClockRect(clock, "MonthTurnPanel/EndMonthButton", new Vector2(70, -6), new Vector2(132, 44), 20);
            bool narrow = ((RectTransform)transform).rect.width < 740f;
            ClockRect(clock, "MonthTurnFeedbackText", new Vector2(0, narrow ? 120f : 88f),
                new Vector2(240, 64), 18);
            clock.SetAsLastSibling();
        }

        private static void ClockRect(Transform clock, string path, Vector2 position, Vector2 size, int fontSize)
        {
            if (!(clock.Find(path) is RectTransform rect)) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            foreach (Text text in rect.GetComponentsInChildren<Text>(true)) text.fontSize = fontSize;
        }

        private void ApplyVisibility()
        {
            bool visible = GetComponent<WorldMapController>()?.InTownMenu != true;
            foreach (string name in TravelFields) Visible(name, visible && travelExpanded);
            foreach (string name in InformationFields) Visible(name, visible && informationExpanded);
            Visible("MapTravelSurface", visible && travelExpanded);
            Visible("MapInformationSurface", visible && informationExpanded);
            Visible("MapActionSurface", visible);
            Visible("FeedbackText", visible);
            Visible("MapTravelToggle", visible);
            Visible("MapInformationToggle", visible);
            Visible(ReferenceGalleryNavigator.MapIllustrationChildName, visible);
            Visible("TitleText", visible);
        }

        private void Visible(string path, bool visible)
        {
            Transform child = transform.Find(path);
            if (child != null && child.gameObject.activeSelf != visible) child.gameObject.SetActive(visible);
        }

        private void Panel(string name, Vector2 position, Vector2 size)
        {
            Transform child = transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                child = go.transform;
                child.SetAsFirstSibling();
                go.GetComponent<Image>().color = SengokuUiTheme.IndigoPanel;
                go.GetComponent<Image>().raycastTarget = false;
                SengokuUiArt.ApplyIndigoPatternOverlay(child, name + "Pattern");
            }
            Place(name, position, size);
        }

        private void Toggle(string name, string caption, Vector2 position, float width,
            UnityEngine.Events.UnityAction action)
        {
            Transform child = transform.Find(name);
            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(transform, false);
                go.GetComponent<Image>().color = SengokuUiTheme.IndigoRail;
                go.GetComponent<Button>().targetGraphic = go.GetComponent<Image>();
                var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                label.transform.SetParent(go.transform, false);
                RectTransform rect = (RectTransform)label.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
                Text text = label.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 20;
                text.color = SengokuUiTheme.Gold;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                child = go.transform;
            }
            Button button = child.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            Text labelText = child.GetComponentInChildren<Text>(true);
            labelText.text = caption;
            labelText.color = SengokuUiTheme.Gold;
            child.GetComponent<Image>().color = SengokuUiTheme.IndigoRail;
            Place(name, position, new Vector2(width, 40));
        }

        private void Place(string path, Vector2 position, Vector2 size)
        {
            if (!(transform.Find(path) is RectTransform rect)) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
