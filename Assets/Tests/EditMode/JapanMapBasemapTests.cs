using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// 已登记原创底图 japan_geographic_basemap.png 的接入回归：资源字节与 GUID、
    /// 1824×2048 尺寸、按登记纵横比拟合、作为 MapContent 最底层且不拦截射线，
    /// 路线与枢纽层仍在其上。
    /// </summary>
    public sealed class JapanMapBasemapTests
    {
        private const string BasemapPath = "Resources/Generated/japan_geographic_basemap.png";

        private const string ApprovedSha256 =
            "328b29af9b61490eb5dd25bff6439bfbb2afdabb022e2451dc8d3afd1b80f127";

        private const string ApprovedGuid = "ab1674103f114bab9af8256dc0a081bb";

        private GameObject _canvasObject;
        private GameObject _rootObject;
        private GameSession _session;
        private JapanMapScreen _screen;
        private RectTransform _mapArea;
        private RectTransform _mapContent;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = (RectTransform)_canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            canvasRect.position = new Vector3(960f, 540f, 0f);

            _session = GameSession.NewGame();
            _session.transform.SetParent(_canvasObject.transform, false);

            _rootObject = new GameObject("JapanMapRoot", typeof(RectTransform), typeof(Image));
            var rootRect = (RectTransform)_rootObject.transform;
            rootRect.SetParent(_canvasObject.transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            _screen = _rootObject.AddComponent<JapanMapScreen>();
            _screen.Initialize(_session, rootRect);
            _screen.Open();

            _mapArea = (RectTransform)rootRect.Find("JapanMapArea");
            Assert.That(_mapArea, Is.Not.Null, "地图区域必须存在。");
            _mapContent = (RectTransform)_mapArea.Find("MapContent");
            Assert.That(_mapContent, Is.Not.Null, "地图视口必须有直接子级 MapContent。");
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasObject != null)
                UnityEngine.Object.DestroyImmediate(_canvasObject);
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void BasemapAsset_IsTheApprovedPngWithRegisteredGuid()
        {
            string pngPath = Path.Combine(UnityEngine.Application.dataPath, BasemapPath);
            Assert.That(File.Exists(pngPath), Is.True, "缺少已登记的底图 PNG：" + pngPath);

            string hash;
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(pngPath))
            {
                hash = BitConverter.ToString(sha.ComputeHash(stream))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }

            Assert.That(hash, Is.EqualTo(ApprovedSha256),
                "底图字节与已批准源 PNG 不一致。");

            string metaPath = pngPath + ".meta";
            Assert.That(File.Exists(metaPath), Is.True, "缺少底图 .meta：" + metaPath);
            Assert.That(File.ReadAllText(metaPath), Does.Contain("guid: " + ApprovedGuid),
                "底图 .meta 必须保留已登记 GUID。");

            Texture2D texture = Resources.Load<Texture2D>(JapanMapBasemap.ResourcePath);
            Assert.That(texture, Is.Not.Null, "底图必须能从 Resources 加载。");
            Assert.That(texture.width, Is.EqualTo(1824));
            Assert.That(texture.height, Is.EqualTo(2048));

            Sprite sprite = JapanMapBasemap.LoadSprite();
            Assert.That(sprite, Is.Not.Null, "底图必须以 Sprite 形式加载。");
            Assert.That(sprite.rect.width, Is.EqualTo(1824f).Within(0.5f));
            Assert.That(sprite.rect.height, Is.EqualTo(2048f).Within(0.5f));
        }

        [Test]
        public void Basemap_IsBottomLayerOfMapContentAtRegisteredAspect()
        {
            var basemap = (RectTransform)_mapContent.Find("Basemap");
            Assert.That(basemap, Is.Not.Null, "MapContent 下必须有 Basemap 层。");
            Assert.That(basemap.parent, Is.SameAs(_mapContent),
                "底图必须是共享缩放内容的子级，才能随路线与枢纽一起缩放、平移。");
            Assert.That(basemap.GetSiblingIndex(), Is.EqualTo(0),
                "底图必须是 MapContent 的最底层。");

            var image = basemap.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.enabled, Is.True, "已登记底图导入后必须启用。");
            Assert.That(image.sprite, Is.Not.Null, "底图 Image 必须持有精灵。");
            Assert.That(image.raycastTarget, Is.False, "底图不得拦截节点与拖动射线。");
            Assert.That(image.preserveAspect, Is.True, "底图不得横纵非等比拉伸。");

            Vector2 expected = JapanGeoProjection.FitMapSize(_mapArea.rect.size);
            Assert.That(basemap.rect.width, Is.EqualTo(expected.x).Within(0.5f));
            Assert.That(basemap.rect.height, Is.EqualTo(expected.y).Within(0.5f));
            Assert.That(basemap.rect.width / basemap.rect.height,
                Is.EqualTo(1824f / 2048f).Within(1e-3f),
                "底图矩形必须保持 1824×2048 的纵横比。");
            Assert.That(basemap.rect.width, Is.LessThanOrEqualTo(_mapArea.rect.width + 0.5f));
            Assert.That(basemap.rect.height, Is.LessThanOrEqualTo(_mapArea.rect.height + 0.5f));

            var lines = (RectTransform)_mapContent.Find("RouteLines");
            var nodes = (RectTransform)_mapContent.Find("HubNodes");
            Assert.That(lines, Is.Not.Null);
            Assert.That(nodes, Is.Not.Null);
            Assert.That(basemap.GetSiblingIndex(), Is.LessThan(lines.GetSiblingIndex()),
                "底图必须垫在路线下面。");
            Assert.That(lines.GetSiblingIndex(), Is.LessThan(nodes.GetSiblingIndex()),
                "路线必须垫在节点下面。");
        }

        [Test]
        public void Basemap_KeepsItsContentRectWhenTheMapIsRebuilt()
        {
            var basemap = (RectTransform)_mapContent.Find("Basemap");
            Vector2 sizeBefore = basemap.rect.size;
            Vector2 positionBefore = basemap.anchoredPosition;

            _screen.RefreshMapViewport();

            var after = (RectTransform)_mapContent.Find("Basemap");
            Assert.That(after, Is.SameAs(basemap),
                "刷新地图不得重建或替换底图对象。");
            Assert.That(after.rect.size, Is.EqualTo(sizeBefore),
                "刷新地图不得改变底图矩形尺寸。");
            Assert.That(after.anchoredPosition, Is.EqualTo(positionBefore),
                "刷新地图不得移动底图。");
        }
    }
}
