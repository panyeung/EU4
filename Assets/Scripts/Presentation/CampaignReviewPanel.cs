using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// M08 campaign review overlay. It appears only while a real 55-year stage review or
    /// 100-year 百年名门 review is pending, renders only sourced family facts, and offers
    /// exactly one continue action. Acknowledgement is owned by GameSession; this panel
    /// never changes time, resources or the Phase 1 demo completion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CampaignReviewPanel : MonoBehaviour
    {
        /// <summary>Fixed scene root generated beneath the existing Canvas.</summary>
        public const string RootObjectName = "M08CampaignReviewRoot";
        /// <summary>Full-screen overlay page toggled by <see cref="Refresh"/>.</summary>
        public const string PageObjectName = "M08CampaignReviewPage";
        public const string CardObjectName = "M08CampaignReviewCard";
        /// <summary>Suffix that marks the second and later rendered body blocks.</summary>
        public const string BodyBlockNameSuffix = ".block.";

        /// <summary>
        /// Largest character count one rendered body Text may hold. uGUI Text builds a
        /// quad per laid-out character, so a single Text is capped by the CanvasRenderer
        /// mesh limit long before its string ends. The live 55-year review produced
        /// 89,212 characters in one Text, 317,860 vertices, and a blank viewport because
        /// the mesh was rejected. 4,000 characters stay at 16,000 vertices, four times
        /// below the 65,535-vertex limit, while keeping the block count low.
        /// </summary>
        public const int MaxBodyBlockCharacters = 4000;

        /// <summary>Vertex budget one block may use at four vertices per character.</summary>
        public const int MaxBodyBlockVertices = MaxBodyBlockCharacters * 4;

        [Header("Dependencies")]
        [SerializeField] private GameSession gameSession;

        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text contextText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text continueLabelText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button continueButton;

        private bool _subscribed;
        private UnityAction _continueListener;
        private int _staticButtonBindings;
        private readonly List<Text> _bodyBlocks = new List<Text>();
        private string _bodyFullText;
        private ScrollRect _bodyScroll;

        public bool IsBound =>
            gameSession != null && panelRoot != null && titleText != null &&
            contextText != null && bodyText != null && continueButton != null;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;
        public string Title => titleText != null ? titleText.text : string.Empty;
        public string Context => contextText != null ? contextText.text : string.Empty;
        /// <summary>
        /// The whole review body in source order. It is the concatenation of every
        /// rendered block, so no character of the formatted review is dropped.
        /// </summary>
        public string Body => _bodyFullText ?? (bodyText != null ? bodyText.text : string.Empty);
        public string ContinueLabel => continueLabelText != null ? continueLabelText.text : string.Empty;
        public string Status => statusText != null ? statusText.text : string.Empty;
        public int StaticButtonBindingCount => _staticButtonBindings;
        /// <summary>Number of Text blocks the current body is rendered with.</summary>
        public int BodyBlockCount => _bodyBlocks.Count;

        public void BindReferences(GameSession session, GameObject panel, Text title, Text context,
            Text body, Text continueLabel, Text status, Button continueControl)
        {
            Unsubscribe();
            gameSession = session;
            panelRoot = panel;
            titleText = title;
            contextText = context;
            bodyText = body;
            continueLabelText = continueLabel;
            statusText = status;
            continueButton = continueControl;
            Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            if (gameSession == null)
                gameSession = FindAnyObjectByType<GameSession>();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>Rebuilds the overlay from the current pending review; never mutates state.</summary>
        public void Refresh()
        {
            if (!IsBound)
            {
                if (panelRoot != null)
                    panelRoot.SetActive(false);
                return;
            }

            CampaignReviewView review = gameSession.CampaignReview;
            if (!review.IsAvailable)
            {
                panelRoot.SetActive(false);
                return;
            }

            titleText.text = review.Title;
            contextText.text = review.ContextLine + "｜评价日：" + review.ReviewDateLine;
            RenderBody(review.Subtitle + "\n\n" + review.FormatBody());
            if (continueLabelText != null)
                continueLabelText.text = review.ContinueLabel;
            SetStatus(gameSession.LastCampaignReviewMessage);
            panelRoot.SetActive(true);
        }

        /// <summary>
        /// Renders one body into the vertically scrolling content as bounded Text blocks.
        /// The generated body Text is the first block and the panel clones it for the rest,
        /// so every block shares the reviewed font, size and colour and no single Text mesh
        /// grows past <see cref="MaxBodyBlockCharacters"/>. An unchanged body is left alone
        /// so later state notifications do not rebuild the blocks.
        /// </summary>
        internal void RenderBody(string body)
        {
            string text = body ?? string.Empty;
            if (bodyText == null)
                return;
            bool blocksMatchTemplate = _bodyBlocks.Count > 0 && _bodyBlocks[0] == bodyText;
            if (_bodyFullText == text && blocksMatchTemplate)
                return;

            IReadOnlyList<string> blocks = SplitBody(text, MaxBodyBlockCharacters);
            EnsureBodyBlocks(blocks.Count);
            _bodyFullText = text;
            for (int i = 0; i < _bodyBlocks.Count; i++)
            {
                Text block = _bodyBlocks[i];
                if (block == null)
                    continue;
                block.text = blocks[i];
                block.gameObject.SetActive(true);
            }
            ResetBodyScroll();
        }

        /// <summary>
        /// Puts a newly rendered body back at its first line. The 100-year review follows an
        /// acknowledged 55-year review in the same scroll, so keeping the old offset would open
        /// it partway down. The generated content is anchored and pivoted at the top, where an
        /// anchored Y of zero is exactly the first line.
        /// </summary>
        private void ResetBodyScroll()
        {
            ScrollRect scroll = BodyScroll;
            if (scroll == null)
                return;
            scroll.StopMovement();
            if (scroll.content == null)
                return;
            Vector2 position = scroll.content.anchoredPosition;
            position.y = 0f;
            scroll.content.anchoredPosition = position;
        }

        private ScrollRect BodyScroll
        {
            get
            {
                if (_bodyScroll == null && bodyText != null)
                    _bodyScroll = bodyText.GetComponentInParent<ScrollRect>(true);
                return _bodyScroll;
            }
        }

        /// <summary>
        /// Splits one review body into ordered blocks that each stay under the uGUI Text
        /// mesh limit. A block ends at a line break when one occurs inside its window, so a
        /// history row is never cut in half, and a surrogate pair is never separated.
        /// Concatenating the blocks in order reproduces <paramref name="body"/> exactly.
        /// </summary>
        public static IReadOnlyList<string> SplitBody(string body, int maxCharacters)
        {
            if (maxCharacters < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCharacters));
            if (string.IsNullOrEmpty(body))
                return new[] { string.Empty };

            var blocks = new List<string>((body.Length / maxCharacters) + 1);
            int start = 0;
            while (start < body.Length)
            {
                int end = start + maxCharacters;
                if (end >= body.Length)
                {
                    blocks.Add(body.Substring(start));
                    break;
                }

                int lineBreak = body.LastIndexOf('\n', end - 1, end - start);
                if (lineBreak > start)
                    end = lineBreak + 1;
                if (char.IsHighSurrogate(body[end - 1]) && char.IsLowSurrogate(body[end]))
                    end--;
                if (end <= start)
                    end = start + maxCharacters;
                blocks.Add(body.Substring(start, end - start));
                start = end;
            }
            return blocks;
        }

        private void EnsureBodyBlocks(int count)
        {
            for (int i = _bodyBlocks.Count - 1; i >= 0; i--)
                if (_bodyBlocks[i] == null)
                    _bodyBlocks.RemoveAt(i);
            if (_bodyBlocks.Count == 0)
                _bodyBlocks.Add(bodyText);
            else if (_bodyBlocks[0] != bodyText)
            {
                for (int i = 1; i < _bodyBlocks.Count; i++)
                    DestroyBodyBlock(_bodyBlocks[i]);
                _bodyBlocks.Clear();
                _bodyBlocks.Add(bodyText);
            }

            while (_bodyBlocks.Count < count)
                _bodyBlocks.Add(CreateBodyBlock(_bodyBlocks.Count));
            while (_bodyBlocks.Count > count)
            {
                int last = _bodyBlocks.Count - 1;
                Text extra = _bodyBlocks[last];
                _bodyBlocks.RemoveAt(last);
                DestroyBodyBlock(extra);
            }
        }

        private static void DestroyBodyBlock(Text block)
        {
            if (block == null)
                return;
            block.gameObject.SetActive(false);
            if (UnityEngine.Application.isPlaying)
                UnityEngine.Object.Destroy(block.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(block.gameObject);
        }

        private Text CreateBodyBlock(int index)
        {
            Text block = Instantiate(bodyText, bodyText.transform.parent);
            block.gameObject.name = bodyText.name + BodyBlockNameSuffix + index;
            block.transform.SetAsLastSibling();
            block.rectTransform.anchoredPosition = Vector2.zero;
            block.rectTransform.localScale = Vector3.one;
            block.rectTransform.localRotation = Quaternion.identity;
            block.text = string.Empty;
            block.raycastTarget = false;
            return block;
        }

        /// <summary>
        /// Acknowledges the single pending review. Failure keeps the overlay open and the
        /// message explains why; success persists the flag and closes this review.
        /// </summary>
        public void Continue()
        {
            if (gameSession == null)
                return;
            CampaignReviewResult result = gameSession.AcknowledgeCampaignReview();
            SetStatus(result.Message);
            Refresh();
            if (result.Succeeded && statusText != null)
                statusText.text = result.Message;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message ?? string.Empty;
        }

        private void Subscribe()
        {
            if (gameSession == null || _subscribed)
                return;
            gameSession.SessionStateChanged += OnSessionStateChanged;
            _subscribed = true;
            if (continueButton != null && _continueListener == null)
            {
                _continueListener = Continue;
                continueButton.onClick.AddListener(_continueListener);
                _staticButtonBindings++;
            }
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
            if (_continueListener != null)
            {
                if (continueButton != null)
                    continueButton.onClick.RemoveListener(_continueListener);
                _continueListener = null;
                _staticButtonBindings--;
            }
            if (_staticButtonBindings < 0)
                _staticButtonBindings = 0;
        }

        private void OnSessionStateChanged()
        {
            Refresh();
        }
    }
}
