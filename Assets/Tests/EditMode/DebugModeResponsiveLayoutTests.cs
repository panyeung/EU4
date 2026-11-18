#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class DebugModeResponsiveLayoutTests
    {
        [UnityTest]
        public IEnumerator Window_RefitsAfterTheCanvasShrinksAndKeepsDesignedSizeWhenThereIsRoom()
        {
            var canvas = new GameObject("DebugResponsiveCanvas", typeof(RectTransform));
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            GameSession session = GameSession.NewGame();
            DebugModePanel panel = null;
            try
            {
                panel = DebugModePanel.CreateWindowUnder(canvas.transform, session);
                panel.Open();
                Canvas.ForceUpdateCanvases();

                RectTransform window = (RectTransform)panel.transform;
                Assert.That(window.rect.width * Mathf.Abs(window.localScale.x),
                    Is.EqualTo(1200f).Within(0.5f));
                Assert.That(window.rect.height * Mathf.Abs(window.localScale.y),
                    Is.EqualTo(680f).Within(0.5f));

                canvasRect.sizeDelta = new Vector2(1024f, 576f);
                yield return null;
                Canvas.ForceUpdateCanvases();

                float renderedWidth = window.rect.width * Mathf.Abs(window.localScale.x);
                float renderedHeight = window.rect.height * Mathf.Abs(window.localScale.y);
                Assert.That(renderedWidth, Is.LessThanOrEqualTo(1024.5f));
                Assert.That(renderedHeight, Is.LessThanOrEqualTo(576.5f));
                Assert.That(renderedWidth, Is.EqualTo(1016.47f).Within(0.5f));
                Assert.That(renderedHeight, Is.EqualTo(576f).Within(0.5f));
            }
            finally
            {
                if (panel != null)
                    Object.DestroyImmediate(panel.gameObject);
                Object.DestroyImmediate(session.gameObject);
                Object.DestroyImmediate(canvas);
            }
        }
    }
}
#endif
