using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// M06 tactical infiltration page. It renders the fixed 8x6 grid, the two
    /// operatives, the turn count, guard facing and sight, the alert display, the
    /// objective, the extraction cells, the result and the exact applied resources.
    /// Every command goes through <see cref="GameSession"/>; opening, closing and
    /// refreshing never advance a turn, move a guard or change campaign state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TacticalMissionPanel : MonoBehaviour
    {
        /// <summary>Fixed scene root the generated infiltration UI hangs from.</summary>
        public const string RootObjectName = "M6TacticalMissionRoot";

        /// <summary>Canvas-level modal overlay that owns the scrim and the mission sheet.</summary>
        public const string OverlayObjectName = "M6TacticalMissionOverlay";

        /// <summary>Generated infiltration page; it owns the parchment sheet.</summary>
        public const string PageObjectName = "M6TacticalMissionPage";

        /// <summary>Non-colour cue that every cell in guard sight appends to its label.</summary>
        public const string SightGlyph = "目";

        private const string WallGlyph = "壁";
        private const string CoverGlyph = "掩";
        private const string ExtractionGlyph = "撤";

        /// <summary>
        /// Floor for the fitted page. It keeps a pathological parent from collapsing the
        /// page into an unreadable thumbnail; below this the town band itself is unusable.
        /// </summary>
        public const float MinimumPageScale = 0.25f;

        [SerializeField] private GameSession gameSession;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button northButton;
        [SerializeField] private Button eastButton;
        [SerializeField] private Button southButton;
        [SerializeField] private Button westButton;
        [SerializeField] private Button moveButton;
        [SerializeField] private Button sprintButton;
        [SerializeField] private Button waitButton;
        [SerializeField] private Button interactButton;
        [SerializeField] private Button extractButton;
        [SerializeField] private Button withdrawButton;
        [SerializeField] private Button switchButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text dateText;
        [SerializeField] private Text resourceText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Text teamText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text guardText;
        [SerializeField] private Text resultText;
        [SerializeField] private Text eventText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text chronicleText;
        [SerializeField] private Text selectionText;
        [SerializeField] private GameObject gridRoot;
        [SerializeField] private Image[] cellImages;
        [SerializeField] private Text[] cellLabels;
        [SerializeField] private Transform candidateContainer;
        [SerializeField] private GameObject candidateTemplate;
        [SerializeField] private GameObject candidateScroll;

        private readonly List<GameObject> _candidates = new List<GameObject>();
        private readonly List<GameObject> _retiredCandidates = new List<GameObject>();
        private readonly List<PersonId> _candidatePersons = new List<PersonId>();
        private readonly List<GameObject> _optionRows = new List<GameObject>();
        private readonly List<PersonId> _selected = new List<PersonId>(2);
        private MissionOperationMode _mode = MissionOperationMode.Manual;
        private MissionPreparation _preparation = MissionPreparation.None;
        private PersonId _captain;
        private readonly bool[] _cellSight =
            new bool[TacticalMissionScenario.Width * TacticalMissionScenario.Height];
        private WorldMapController _worldMapController;
        private TacticalDirection _direction = TacticalDirection.North;
        private int _activeSlot;
        private bool _isOpen;
        private bool _subscribed;
        // Parent size the sheet was last fitted to. LateUpdate compares it with the live
        // overlay rect, so a window or canvas resize while the page is open re-fits the
        // sheet without waiting for the next refresh.
        private Vector2 _fittedParentSize = new Vector2(-1f, -1f);

        /// <summary>Why a cell deviates from its terrain fill.</summary>
        public enum CellHighlight
        {
            None = 0,
            Dispatch = 1,
            Operative = 2,
            Guard = 3,
            GuardAlert = 4
        }

        // Cell fills stay in the indigo family and dark enough for the pale label that
        // sits on every cell: the darkest pairing here still measures above 6:1, which
        // is what keeps the 13 px glyphs readable on a phone-sized window.
        private static readonly Color FloorFill = new Color32(0x24, 0x2E, 0x3E, 0xFF);
        private static readonly Color WallFill = new Color32(0x2A, 0x2E, 0x3A, 0xFF);
        private static readonly Color CoverFill = new Color32(0x2E, 0x45, 0x3C, 0xFF);
        private static readonly Color ExtractionFill = new Color32(0x2C, 0x46, 0x2C, 0xFF);
        private static readonly Color DispatchFill = new Color32(0x6B, 0x55, 0x20, 0xFF);
        private static readonly Color OperativeFill = new Color32(0x2E, 0x4A, 0x6B, 0xFF);
        private static readonly Color GuardFill = new Color32(0x5E, 0x3E, 0x16, 0xFF);
        private static readonly Color GuardAlertFill = new Color32(0x6E, 0x2A, 0x24, 0xFF);

        public bool IsOpen => _isOpen;
        public bool OverlayVisible => overlayRoot != null && overlayRoot.activeSelf;
        public int RefreshCount { get; private set; }
        public int ActiveSlot => _activeSlot;
        public int SelectedCount => _selected.Count;

        public bool IsBound =>
            gameSession != null && panelRoot != null && openButton != null &&
            closeButton != null && startButton != null && northButton != null &&
            eastButton != null && southButton != null && westButton != null &&
            moveButton != null && sprintButton != null && waitButton != null &&
            interactButton != null && extractButton != null && withdrawButton != null &&
            switchButton != null && titleText != null && stateText != null &&
            dateText != null && resourceText != null && objectiveText != null &&
            teamText != null && statusText != null && guardText != null &&
            resultText != null && eventText != null && messageText != null &&
            chronicleText != null && selectionText != null && gridRoot != null &&
            cellImages != null && cellImages.Length ==
            TacticalMissionScenario.Width * TacticalMissionScenario.Height &&
            cellLabels != null && cellLabels.Length ==
            TacticalMissionScenario.Width * TacticalMissionScenario.Height &&
            candidateContainer != null && candidateTemplate != null &&
            candidateScroll != null;

        public string StateLine => TextOf(stateText);
        public string DateLine => TextOf(dateText);
        public string ResourceLine => TextOf(resourceText);
        public string ObjectiveLine => TextOf(objectiveText);
        public string TeamLine => TextOf(teamText);
        public string StatusLine => TextOf(statusText);
        public string GuardLine => TextOf(guardText);
        public string ResultLine => TextOf(resultText);
        public string EventLine => TextOf(eventText);
        public string MessageLine => TextOf(messageText);
        public string ChronicleLine => TextOf(chronicleText);
        public string SelectionLine => TextOf(selectionText);

        public bool IsGridVisible => gridRoot != null && gridRoot.activeSelf;
        public bool IsCandidateListVisible =>
            candidateScroll != null && candidateScroll.activeSelf;

        public bool StartInteractable => startButton != null && startButton.interactable;
        public bool MoveInteractable => moveButton != null && moveButton.interactable;
        public bool SprintInteractable => sprintButton != null && sprintButton.interactable;
        public bool WaitInteractable => waitButton != null && waitButton.interactable;
        public bool InteractInteractable => interactButton != null && interactButton.interactable;
        public bool ExtractInteractable => extractButton != null && extractButton.interactable;
        public bool WithdrawInteractable => withdrawButton != null && withdrawButton.interactable;
        public bool SwitchInteractable => switchButton != null && switchButton.interactable;

        public int CandidateCount => _candidates.Count;

        public string CandidateLabel(int index)
        {
            if (index < 0 || index >= _candidates.Count || _candidates[index] == null)
                return string.Empty;
            Text label = _candidates[index].GetComponentInChildren<Text>(true);
            return label != null ? label.text : string.Empty;
        }

        public bool CandidateSelected(int index)
        {
            if (index < 0 || index >= _candidatePersons.Count)
                return false;
            return _selected.Contains(_candidatePersons[index]);
        }

        public PersonId CandidatePerson(int index) =>
            index >= 0 && index < _candidatePersons.Count ? _candidatePersons[index] : default;

        public int CellCount => cellImages != null ? cellImages.Length : 0;

        public string CellLabel(int x, int y)
        {
            int index = CellIndex(x, y);
            return index >= 0 && index < cellLabels.Length && cellLabels[index] != null
                ? cellLabels[index].text
                : string.Empty;
        }

        public Color CellColor(int x, int y)
        {
            int index = CellIndex(x, y);
            return index >= 0 && index < cellImages.Length && cellImages[index] != null
                ? cellImages[index].color
                : default;
        }

        public bool CellInGuardSight(int x, int y)
        {
            int index = CellIndex(x, y);
            return index >= 0 && index < _cellSight.Length && _cellSight[index];
        }

        public void BindReferences(GameSession session, GameObject panel, Button open,
            Button close, Button start, Button north, Button east, Button south, Button west,
            Button move, Button sprint, Button wait, Button interact, Button extract,
            Button withdraw, Button switchOperative, Text title, Text state, Text date,
            Text resources, Text objective, Text team, Text status, Text guard, Text result,
            Text eventLine, Text message, Text chronicle, Text selection, GameObject grid,
            Image[] cells, Text[] labels, Transform candidates, GameObject template,
            GameObject candidateList)
        {
            Unsubscribe();
            ClearCandidates();
            gameSession = session;
            panelRoot = panel;
            openButton = open;
            closeButton = close;
            startButton = start;
            northButton = north;
            eastButton = east;
            southButton = south;
            westButton = west;
            moveButton = move;
            sprintButton = sprint;
            waitButton = wait;
            interactButton = interact;
            extractButton = extract;
            withdrawButton = withdraw;
            switchButton = switchOperative;
            titleText = title;
            stateText = state;
            dateText = date;
            resourceText = resources;
            objectiveText = objective;
            teamText = team;
            statusText = status;
            guardText = guard;
            resultText = result;
            eventText = eventLine;
            messageText = message;
            chronicleText = chronicle;
            selectionText = selection;
            gridRoot = grid;
            cellImages = cells;
            cellLabels = labels;
            candidateContainer = candidates;
            candidateTemplate = template;
            candidateScroll = candidateList;
            _selected.Clear();
            _activeSlot = 0;
            _isOpen = false;
            SetActive(page: false);
            if (openButton != null)
                openButton.gameObject.SetActive(true);
            Subscribe();
            Refresh();
        }

        /// <summary>
        /// Binds the canvas-level overlay that carries the scrim and the mission sheet. The
        /// overlay is shown and hidden with the page so the sheet never renders over the town
        /// band without its opaque surface, and a null overlay keeps the plain page behavior
        /// used by unit fixtures.
        /// </summary>
        public void BindOverlay(GameObject overlay)
        {
            overlayRoot = overlay;
            if (overlayRoot != null && !_isOpen)
                overlayRoot.SetActive(false);
        }

        private void SetActive(bool page)
        {
            if (panelRoot != null)
                panelRoot.SetActive(page);
            if (overlayRoot != null)
                overlayRoot.SetActive(page);
        }

        /// <summary>Rebinds after a load or session swap without touching gameplay state.</summary>
        public void Bind(GameSession session)
        {
            if (gameSession != session)
            {
                Unsubscribe();
                gameSession = session;
                _isOpen = false;
                _selected.Clear();
                _activeSlot = 0;
                SetActive(page: false);
                ClearCandidates();
            }
            Subscribe();
            Refresh();
        }

        public void BindController(WorldMapController controller)
        {
            _worldMapController = controller;
        }

        public void OpenFromEntryButton()
        {
            if (_worldMapController != null)
            {
                _worldMapController.ToggleTacticalMissionPanel();
                return;
            }
            Open();
        }

        public void Open()
        {
            _isOpen = true;
            // The sheet lives in the canvas-level overlay, so raising the overlay is what
            // actually lifts the modal above the map view. Moving the town-rooted panel
            // component would be inert here.
            RaiseOverlay();
            SetActive(page: true);
            if (openButton != null)
                openButton.gameObject.SetActive(false);
            Refresh();
        }

        public void Close()
        {
            _isOpen = false;
            ClearCandidates();
            SetActive(page: false);
            if (openButton != null)
                openButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Read-only projection of the operation. Refreshing never advances a turn,
        /// moves a guard, spends resources or writes a chronicle record.
        /// </summary>
        public void Refresh()
        {
            RefreshCount++;
            // The saved scene must keep its built-in font reference, because an OS font
            // cannot be serialized. At runtime the page switches to the project's CJK
            // serif, which is what makes the authored type legible. The sheet lives in the
            // canvas-level overlay, so its subtree is dressed separately from the town root.
            if (UnityEngine.Application.isPlaying)
            {
                SengokuUiTheme.ApplyFont(transform);
                if (panelRoot != null)
                    SengokuUiTheme.ApplyFont(panelRoot.transform);
            }
            // Candidate rows are owned by this refresh: rebuilding without clearing first
            // left the previous set active and interactable, so every selection click (or
            // any session notification during the briefing) stacked a duplicate set.
            ClearCandidates();
            FitPageToParent();
            if (gameSession == null)
            {
                FillUnavailable();
                return;
            }

            TacticalMissionSnapshot snapshot = gameSession.TacticalMissionStatus;
            IndustryResources resources = gameSession.Industry.Resources;
            bool completed = gameSession.IsTacticalMissionCompleted;
            bool briefing = snapshot == null;

            SetText(titleText, "潜入密令：夺取密报");
            SetText(stateText, "状态：" + StateLabel(gameSession, snapshot, completed));
            SetText(dateText, "日期：" + CurrentDate() + "（" +
                              LocationId.GetChineseName(gameSession.CurrentLocation) + "）");
            SetText(resourceText, "资金 " + resources.Funds + "　物资 " + resources.Supplies +
                                  "　情报 " + resources.Intelligence);
            int plannedCost = TacticalMissionResolutionRules.RequiredStartingSupplies(_preparation);
            // One short authored line per rule keeps every line inside the 560-unit body
            // column, so the briefing block can be sized once and never clipped. The long
            // run-on sentence this replaced wrapped into far more lines than the page
            // reserved and truncated on screen.
            SetText(objectiveText, "目标：夺取 (6,4) 的密报，再从 (0,0)、(1,0) 撤离。\n" +
                                   "执行方式：" + MissionOutcomeTable.ModeName(_mode) + "｜准备：" +
                                   MissionOutcomeTable.PreparationName(_preparation) + "\n" +
                                   "结算：结束扣除 2 物资（侦察准备再 +1，共 " + plannedCost + "）。\n" +
                                   "完全成功：+4 资金、+6 情报、名望 +3、关系 +5、隐蔽度 +3。\n" +
                                   "成功但有代价：再付 1 物资（名望 +2／关系 +2／隐蔽度 +1）。\n" +
                                   "失败并安全撤离：名望 -1／关系 -3／隐蔽度 -2。\n" +
                                   "严重失败：名望 -3／关系 -5／隐蔽度 -5。\n" +
                                   "简报逐名披露死亡风险：" + BuildRiskLine() + "。\n" +
                                   "严重失败时每名队员独立进行 10% 死亡检定，幸存者负伤。\n" +
                                   ( _preparation == MissionPreparation.Scouting
                                       ? "侦察路线：巡逻守卫沿 (7,1) → (7,0) 环线巡逻。\n"
                                       : "未侦察：只显示当前已知守卫位置。\n") +
                                   "潜入期间无法存读档。");
            SetText(teamText, BuildTeamLine(snapshot));
            SetText(statusText, BuildStatusLine(snapshot));
            SetText(guardText, BuildGuardLine(snapshot));
            SetText(resultText, BuildResultLine(snapshot, completed));
            SetText(eventText, snapshot != null ? snapshot.LastEvent : "事件：尚未开始。");
            SetText(messageText, gameSession.LastTacticalMessage);
            SetText(chronicleText, BuildChronicleLine());
            SetText(selectionText, BuildSelectionLine(snapshot));

            RenderGrid(snapshot);
            SetActive(teamText != null ? teamText.gameObject : null, !briefing);
            SetActive(selectionText != null ? selectionText.gameObject : null,
                briefing && !completed);
            bool showOperationLines = !briefing || completed;
            SetActive(statusText != null ? statusText.gameObject : null, !briefing);
            SetActive(guardText != null ? guardText.gameObject : null, !briefing);
            SetActive(eventText != null ? eventText.gameObject : null, !briefing);
            SetActive(resultText != null ? resultText.gameObject : null, showOperationLines);
            SetActive(chronicleText != null ? chronicleText.gameObject : null,
                showOperationLines);

            if (gridRoot != null)
                gridRoot.SetActive(true);
            if (candidateScroll != null)
                candidateScroll.SetActive(briefing && !completed);

            bool active = snapshot != null && snapshot.IsActive;
            SetActive(startButton != null ? startButton.gameObject : null,
                briefing && !completed);
            SetInteractable(startButton, briefing && !completed &&
                                           gameSession.CanBeginTacticalMission &&
                                           resources.Supplies >=
                                           TacticalMissionResolutionRules
                                               .RequiredStartingSupplies(_preparation) &&
                                           _selected.Count == TacticalMissionScenario.OperativeCount);
            bool canAct = active && _isOpen && isActiveAndEnabled;
            SetInteractable(northButton, canAct);
            SetInteractable(eastButton, canAct);
            SetInteractable(southButton, canAct);
            SetInteractable(westButton, canAct);
            SetInteractable(moveButton, canAct);
            SetInteractable(sprintButton, canAct);
            SetInteractable(waitButton, canAct);
            SetInteractable(switchButton, canAct);
            SetInteractable(interactButton, canAct && CanInteract(snapshot));
            SetInteractable(extractButton, canAct && CanExtract(snapshot));
            SetInteractable(withdrawButton, canAct);

            if (_isOpen && briefing && !completed)
            {
                BuildCandidates();
                BuildOptionRows();
            }
            else
                ClearCandidates();
        }

        public void SelectDirection(TacticalDirection direction)
        {
            _direction = direction;
            Refresh();
        }

        public void ClickNorth() => SelectDirection(TacticalDirection.North);
        public void ClickEast() => SelectDirection(TacticalDirection.East);
        public void ClickSouth() => SelectDirection(TacticalDirection.South);
        public void ClickWest() => SelectDirection(TacticalDirection.West);

        public void SwitchOperative()
        {
            _activeSlot = (_activeSlot + 1) % TacticalMissionScenario.OperativeCount;
            Refresh();
        }

        public void ClickStart()
        {
            if (gameSession == null || _selected.Count != TacticalMissionScenario.OperativeCount)
            {
                Refresh();
                return;
            }
            PersonId captain = CaptainId();
            TacticalActionResult result = _mode == MissionOperationMode.Manual
                ? gameSession.TryBeginTacticalMission(_selected[0], _selected[1], captain,
                    _preparation)
                : gameSession.TryDelegateTacticalMission(_selected[0], _selected[1], captain,
                    _preparation);
            if (!result.Succeeded)
            {
                if (_isOpen)
                    Refresh();
                return;
            }
            _selected.Clear();
            _captain = default;
            Refresh();
        }

        public void ClickMove() => Submit(operative => gameSession.TryTacticalMove(operative, _direction));
        public void ClickSprint() => Submit(operative => gameSession.TryTacticalSprint(operative, _direction));
        public void ClickWait() => Submit(operative => gameSession.TryTacticalWait(operative));
        public void ClickInteract() => Submit(operative => gameSession.TryTacticalInteract(operative));
        public void ClickExtract() => Submit(_ => gameSession.TryTacticalExtract());
        public void ClickWithdraw() => Submit(_ => gameSession.TryTacticalWithdraw());

        public void ClickCandidate(int index)
        {
            if (index < 0 || index >= _candidatePersons.Count || gameSession == null)
                return;
            PersonId personId = _candidatePersons[index];
            if (!CandidateInteractable(index))
                return;
            if (_selected.Contains(personId))
                _selected.Remove(personId);
            else if (_selected.Count < TacticalMissionScenario.OperativeCount)
                _selected.Add(personId);
            Refresh();
        }

        public bool CandidateInteractable(int index)
        {
            if (index < 0 || index >= _candidates.Count || _candidates[index] == null)
                return false;
            Button button = _candidates[index].GetComponent<Button>();
            return button != null && button.interactable;
        }

        private void Submit(System.Func<PersonId, TacticalActionResult> command)
        {
            if (gameSession == null)
            {
                Refresh();
                return;
            }
            TacticalMissionSnapshot snapshot = gameSession.TacticalMissionStatus;
            if (snapshot == null || !snapshot.IsActive)
            {
                Refresh();
                return;
            }
            if (_activeSlot < 0 || _activeSlot >= snapshot.Operatives.Count)
                _activeSlot = 0;
            TacticalActionResult result = command(snapshot.Operatives[_activeSlot].PersonId);
            // A successful command publishes SessionStateChanged; only rejections need a
            // local refresh so their reason is visible exactly once.
            if (!result.Succeeded && _isOpen)
                Refresh();
        }

        private void RenderGrid(TacticalMissionSnapshot snapshot)
        {
            if (cellImages == null || cellLabels == null)
                return;
            for (int y = 0; y < TacticalMissionScenario.Height; y++)
            {
                for (int x = 0; x < TacticalMissionScenario.Width; x++)
                {
                    int index = y * TacticalMissionScenario.Width + x;
                    if (index >= cellImages.Length || index >= cellLabels.Length)
                        continue;
                    var point = new TacticalGridPoint(x, y);
                    TacticalTileKind kind = snapshot != null
                        ? snapshot.TileAt(x, y).Kind
                        : TacticalMissionRules.TileAt(point);
                    bool seen = snapshot != null && snapshot.TileAt(x, y).InGuardSight;
                    _cellSight[index] = seen;
                    MarkCell(snapshot, point, kind, seen, cellImages[index], cellLabels[index]);
                }
            }
        }

        /// <summary>
        /// Fill for one cell. Terrain owns the base tone and a marker overrides it with the
        /// occupant tone; an unmarked cell inside guard sight then leans towards vermilion.
        /// The tint is never the only cue, because <see cref="CellMarker"/> carries
        /// <see cref="SightGlyph"/> on the same cell, so the sight area also reads in
        /// monochrome. Every result stays dark enough for the pale cell label.
        /// </summary>
        public static Color CellFill(TacticalTileKind kind, CellHighlight highlight,
            bool inGuardSight)
        {
            Color fill;
            switch (highlight)
            {
                case CellHighlight.Dispatch:
                    fill = DispatchFill;
                    break;
                case CellHighlight.Operative:
                    fill = OperativeFill;
                    break;
                case CellHighlight.Guard:
                    fill = GuardFill;
                    break;
                case CellHighlight.GuardAlert:
                    fill = GuardAlertFill;
                    break;
                default:
                    fill = TileFill(kind);
                    break;
            }
            return inGuardSight && highlight == CellHighlight.None
                ? Color.Lerp(fill, SengokuUiTheme.Vermilion, 0.22f)
                : fill;
        }

        /// <summary>
        /// Composed cell label. The terrain or occupant glyph stays in front so the cell
        /// keeps its meaning, and every cell the guard can see appends
        /// <see cref="SightGlyph"/>: a sighted wall reads 壁目, a sighted dispatch 报目, a
        /// sighted operative 忍一目, and a sighted empty floor cell simply 目.
        /// </summary>
        public static string CellMarker(TacticalTileKind kind, string occupant,
            bool inGuardSight)
        {
            string marker = !string.IsNullOrEmpty(occupant)
                ? occupant
                : kind == TacticalTileKind.Wall ? WallGlyph
                : kind == TacticalTileKind.Cover ? CoverGlyph
                : kind == TacticalTileKind.Extraction ? ExtractionGlyph
                : string.Empty;
            return inGuardSight ? marker + SightGlyph : marker;
        }

        private static Color TileFill(TacticalTileKind kind)
        {
            switch (kind)
            {
                case TacticalTileKind.Wall: return WallFill;
                case TacticalTileKind.Cover: return CoverFill;
                case TacticalTileKind.Extraction: return ExtractionFill;
                default: return FloorFill;
            }
        }

        private static void MarkCell(TacticalMissionSnapshot snapshot, TacticalGridPoint point,
            TacticalTileKind kind, bool seen, Image image, Text label)
        {
            string occupant = string.Empty;
            CellHighlight highlight = CellHighlight.None;

            if (snapshot != null)
            {
                if (!snapshot.DispatchTaken &&
                    snapshot.DispatchPosition == point)
                {
                    occupant = "报";
                    highlight = CellHighlight.Dispatch;
                }
                for (int i = 0; i < snapshot.Operatives.Count; i++)
                {
                    if (snapshot.Operatives[i].Position != point)
                        continue;
                    occupant = snapshot.Operatives[i].CarryingDispatch ? "忍报" :
                               i == 0 ? "忍一" : "忍二";
                    highlight = CellHighlight.Operative;
                }
                for (int i = 0; i < snapshot.Guards.Count; i++)
                {
                    if (snapshot.Guards[i].Position != point)
                        continue;
                    occupant = "守" + (i == 0 ? "一" : "二");
                    highlight = snapshot.Guards[i].State == TacticalGuardState.Alert
                        ? CellHighlight.GuardAlert
                        : CellHighlight.Guard;
                }
            }
            else
            {
                // The briefing previews the authored posts before the operation starts.
                if (TacticalMissionScenario.DispatchCell == point)
                {
                    occupant = "报";
                    highlight = CellHighlight.Dispatch;
                }
                if (TacticalMissionScenario.PatrolRoute[0] == point ||
                    TacticalMissionScenario.StationaryGuardCell == point)
                {
                    occupant = "守";
                    highlight = CellHighlight.Guard;
                }
            }

            if (image != null)
                image.color = CellFill(kind, highlight, seen);
            if (label != null)
                label.text = CellMarker(kind, occupant, seen);
        }

        private string BuildTeamLine(TacticalMissionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                int eligible = 0;
                foreach (CommissionAssigneeOption option in gameSession.TacticalOperativeOptions)
                    if (option.Eligible)
                        eligible++;
                return "潜入队：待选择（可参加 " + eligible + " 人，需 2 人且同在清洲）";
            }
            var parts = new List<string>(snapshot.Operatives.Count);
            for (int i = 0; i < snapshot.Operatives.Count; i++)
            {
                TacticalOperativeView operative = snapshot.Operatives[i];
                string name = OperativeName(operative.PersonId);
                string carrying = operative.CarryingDispatch ? "（持密报）" : string.Empty;
                string active = i == _activeSlot ? "▶" : string.Empty;
                parts.Add(active + name + carrying + " @ " + operative.Position);
            }
            return "潜入队：" + string.Join("　", parts) + "（换人切换行动者）";
        }

        private string BuildStatusLine(TacticalMissionSnapshot snapshot)
        {
            string direction = "方向：" +
                               TacticalMissionScenario.DirectionName(_direction) + "（点击方位键）";
            if (snapshot == null)
                return "回合 0｜" + direction;
            string outcome = snapshot.IsActive
                ? "进行中"
                : TacticalMissionScenario.OutcomeName(snapshot.Outcome);
            return "回合 " + snapshot.Turn + "｜" + direction + "｜" + outcome;
        }

        private static string BuildGuardLine(TacticalMissionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                // The briefing preview must name the authored posts, not a copy of the
                // scrambled route the old text carried.
                return "守卫：巡逻守卫 " + TacticalMissionScenario.PatrolRoute[0] + " 朝" +
                       TacticalMissionScenario.DirectionName(
                           TacticalMissionScenario.PatrolGuardFacing) + "；驻守守卫 " +
                       TacticalMissionScenario.StationaryGuardCell + " 朝" +
                       TacticalMissionScenario.DirectionName(
                           TacticalMissionScenario.StationaryGuardFacing) + "。";
            }
            var parts = new List<string>(snapshot.Guards.Count);
            for (int i = 0; i < snapshot.Guards.Count; i++)
            {
                TacticalGuardView guard = snapshot.Guards[i];
                parts.Add("守卫" + (i == 0 ? "一" : "二") + " " + guard.Position + " 朝" +
                          TacticalMissionScenario.DirectionName(guard.Facing) + "｜" +
                          TacticalMissionScenario.GuardStateName(guard.State));
            }
            return string.Join("；", parts);
        }

        private string BuildResultLine(TacticalMissionSnapshot snapshot, bool completed)
        {
            if (snapshot == null)
                return completed
                    ? "结果：本次潜入已完成（编年史已记录）。\n" + BuildLastStandingLine()
                    : "结果：尚未开始。";
            if (snapshot.IsActive)
                return "结果：进行中。";
            string applied = "实际结算：资金 " + Signed(gameSession.TacticalAppliedFunds) +
                             "、物资 " + Signed(gameSession.TacticalAppliedSupplies) +
                             "、情报 " + Signed(gameSession.TacticalAppliedIntelligence);
            return "结果：" + TacticalMissionScenario.OutcomeName(snapshot.Outcome) + "｜" + applied +
                   "\n" + BuildLastStandingLine();
        }

        /// <summary>Shared M07 result and actual before/after values of the last M06 run.</summary>
        private string BuildLastStandingLine()
        {
            IReadOnlyList<MissionRecord> records = gameSession.MissionHistory;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                MissionRecord record = records[i];
                if (record.Kind != MissionKind.TacticalInfiltration)
                    continue;
                var builder = new System.Text.StringBuilder(128);
                builder.Append("共享结果：").Append(MissionOutcomeTable.OutcomeName(record.Outcome))
                    .Append("｜名望 ").Append(record.FameBefore).Append("→")
                    .Append(record.FameAfter).Append("，隐蔽度 ")
                    .Append(record.ConcealmentBefore).Append("→")
                    .Append(record.ConcealmentAfter);
                for (int s = 0; s < record.ParticipantStates.Count; s++)
                {
                    MissionParticipantState state = record.ParticipantStates[s];
                    builder.Append("｜").Append(PersonName(state.PersonId));
                    if (state.AliveBefore && !state.AliveAfter)
                        builder.Append(" 阵亡");
                    else if (state.InjuredAfter)
                        builder.Append(" 负伤");
                    else
                        builder.Append(" 无恙");
                }
                return builder.ToString();
            }
            return "共享结果：尚无 M06 结算记录。";
        }

        private string BuildChronicleLine()
        {
            FamilyChronicleEntry last = null;
            IReadOnlyList<FamilyChronicleEntry> entries = gameSession.ChronicleEntries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                FamilyChronicleEntry entry = entries[i];
                if (entry.Category == ChronicleCategory.Mission &&
                    string.Equals(entry.SubjectId, FamilyChronicleEntry.TacticalMissionId,
                        System.StringComparison.Ordinal))
                {
                    last = entry;
                    break;
                }
            }
            return last != null
                ? "编年史：" + last.Title + "｜" + last.Summary
                : "编年史：尚无潜入记录。";
        }

        private string BuildSelectionLine(TacticalMissionSnapshot snapshot)
        {
            if (snapshot != null)
                return "提示：每回合只有一名潜入者行动一次；疾行会发出响声。";
            if (gameSession.IsTacticalMissionCompleted)
                return "本次潜入已经结束，不能重复进行。";
            if (gameSession.CurrentLocation != LocationId.Kiyosu)
                return "请返回清洲本家后再开始潜入。";
            PersonId captain = CaptainId();
            string captainText = captain.IsEmpty ? "选择两名成员后选择队长" : PersonName(captain);
            return "已选择 " + _selected.Count + "/" + TacticalMissionScenario.OperativeCount +
                   " 名潜入者（点击家人选择）｜执行方式：" +
                   MissionOutcomeTable.ModeName(_mode) + "｜准备：" +
                   MissionOutcomeTable.PreparationName(_preparation) +
                   "（" + TacticalMissionResolutionRules.RequiredStartingSupplies(_preparation) +
                   " 物资）｜队长：" + captainText + "。\n上列按钮可分别点选执行方式、准备与队长，" +
                   "开始后冻结。";
        }

        private bool CanInteract(TacticalMissionSnapshot snapshot)
        {
            if (snapshot == null || snapshot.DispatchTaken)
                return false;
            if (_activeSlot < 0 || _activeSlot >= snapshot.Operatives.Count)
                return false;
            return TacticalMissionRules.ManhattanDistance(
                       snapshot.Operatives[_activeSlot].Position,
                       snapshot.DispatchPosition) == 1;
        }

        private static bool CanExtract(TacticalMissionSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.DispatchTaken ||
                snapshot.Operatives.Count != TacticalMissionScenario.OperativeCount)
                return false;
            IReadOnlyList<TacticalGridPoint> cells = TacticalMissionScenario.ExtractionCells;
            TacticalGridPoint first = snapshot.Operatives[0].Position;
            TacticalGridPoint second = snapshot.Operatives[1].Position;
            return (first == cells[0] && second == cells[1]) ||
                   (first == cells[1] && second == cells[0]);
        }

        private string BuildRiskLine()
        {
            if (_selected.Count == 0)
                return "选择两名出发成员后逐名列出";
            var names = new List<string>(_selected.Count);
            for (int i = 0; i < _selected.Count; i++)
                names.Add(PersonName(_selected[i]));
            return string.Join("、", names);
        }

        private string PersonName(PersonId personId)
        {
            if (gameSession == null || personId.IsEmpty)
                return "—";
            SuccessionState succession = gameSession.Succession;
            return succession != null && succession.World != null
                ? IndustryService.PersonName(succession, personId)
                : personId.Value;
        }

        /// <summary>
        /// Explicit player choices built at runtime from the authored candidate template
        /// under this panel's existing candidate container. Two rows each for operation
        /// mode and preparation, plus one captain row per selected member. No scene or
        /// prefab is edited, and the choice freezes when the mission starts.
        /// </summary>
        private void BuildOptionRows()
        {
            if (candidateContainer == null || candidateTemplate == null)
                return;
            bool interactable = _isOpen && isActiveAndEnabled;
            AddModeRow(MissionOperationMode.Manual, interactable);
            AddModeRow(MissionOperationMode.Delegated, interactable);
            AddPreparationRow(MissionPreparation.None, interactable);
            AddPreparationRow(MissionPreparation.Scouting, interactable);
            for (int i = 0; i < _selected.Count; i++)
                AddCaptainRow(_selected[i], interactable);
            if (candidateContainer is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private void AddModeRow(MissionOperationMode mode, bool interactable)
        {
            GameObject row = Instantiate(candidateTemplate, candidateContainer);
            row.name = "TacticalMode_" + mode;
            row.SetActive(true);
            Text label = row.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = (_mode == mode ? "✔ " : string.Empty) +
                             (mode == MissionOperationMode.Manual
                                 ? "亲自执行（进入战术潜入）"
                                 : "自动委派（立即按冻结数值结算）");
            Button button = row.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
                MissionOperationMode captured = mode;
                button.onClick.AddListener(() => SelectMode(captured));
            }
            _optionRows.Add(row);
        }

        private void AddPreparationRow(MissionPreparation preparation, bool interactable)
        {
            GameObject row = Instantiate(candidateTemplate, candidateContainer);
            row.name = "TacticalPreparation_" + preparation;
            row.SetActive(true);
            Text label = row.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = (_preparation == preparation ? "✔ " : string.Empty) +
                             (preparation == MissionPreparation.None
                                 ? "不准备（额外 0 物资，准备加值 0）"
                                 : "侦察路线（额外 1 物资，准备加值 +10）");
            Button button = row.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
                MissionPreparation captured = preparation;
                button.onClick.AddListener(() => SelectPreparation(captured));
            }
            _optionRows.Add(row);
        }

        private void AddCaptainRow(PersonId personId, bool interactable)
        {
            GameObject row = Instantiate(candidateTemplate, candidateContainer);
            row.name = "TacticalCaptain_" + personId.Value;
            row.SetActive(true);
            bool selected = CaptainId() == personId;
            Text label = row.GetComponentInChildren<Text>(true);
            if (label != null)
                label.text = (selected ? "✔ " : string.Empty) + "队长：" + PersonName(personId);
            Button button = row.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
                PersonId captured = personId;
                button.onClick.AddListener(() => SelectCaptain(captured));
            }
            _optionRows.Add(row);
        }

        private PersonId CaptainId()
        {
            if (_selected.Count == 0)
                return default;
            return _captain.IsEmpty || !_selected.Contains(_captain) ? _selected[0] : _captain;
        }

        private void SelectMode(MissionOperationMode mode)
        {
            _mode = mode;
            if (_isOpen)
                Refresh();
        }

        private void SelectPreparation(MissionPreparation preparation)
        {
            _preparation = preparation;
            if (_isOpen)
                Refresh();
        }

        private void SelectCaptain(PersonId personId)
        {
            _captain = personId;
            if (_isOpen)
                Refresh();
        }

        private void BuildCandidates()
        {
            if (candidateContainer == null || candidateTemplate == null)
                return;

            IReadOnlyList<CommissionAssigneeOption> options =
                gameSession.TacticalOperativeOptions;
            for (int i = 0; i < options.Count; i++)
            {
                CommissionAssigneeOption option = options[i];
                GameObject buttonObject = Instantiate(candidateTemplate, candidateContainer);
                buttonObject.name = "TacticalCandidate_" + option.PersonId.Value;
                buttonObject.SetActive(true);
                Text label = buttonObject.GetComponentInChildren<Text>(true);
                if (label != null)
                {
                    string mark = _selected.Contains(option.PersonId) ? "✔ " : string.Empty;
                    label.text = option.Eligible
                        ? mark + option.DisplayName + "（可参加）"
                        : option.DisplayName + "｜" + option.Reason;
                }
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = option.Eligible && _isOpen && isActiveAndEnabled &&
                                          (_selected.Contains(option.PersonId) ||
                                           _selected.Count < TacticalMissionScenario.OperativeCount);
                    int captured = i;
                    button.onClick.AddListener(() => ClickCandidate(captured));
                }
                _candidates.Add(buttonObject);
                _candidatePersons.Add(option.PersonId);
            }

            if (candidateContainer is RectTransform rect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        private void ClearCandidates()
        {
            // Rows retire one refresh late. A row can still be dispatching its own onClick
            // when the refresh runs, and DestroyImmediate inside that callback destroyed
            // the object under the event system. Deactivating now and destroying at the
            // next clear keeps the callback safe and still leaks nothing.
            DestroyRetiredCandidates();
            for (int i = _candidates.Count - 1; i >= 0; i--)
            {
                GameObject candidate = _candidates[i];
                if (candidate == null)
                    continue;
                candidate.SetActive(false);
                _retiredCandidates.Add(candidate);
            }
            _candidates.Clear();
            _candidatePersons.Clear();
            for (int i = _optionRows.Count - 1; i >= 0; i--)
            {
                GameObject option = _optionRows[i];
                if (option == null)
                    continue;
                option.SetActive(false);
                _retiredCandidates.Add(option);
            }
            _optionRows.Clear();
        }

        private void DestroyRetiredCandidates()
        {
            for (int i = _retiredCandidates.Count - 1; i >= 0; i--)
            {
                GameObject candidate = _retiredCandidates[i];
                if (candidate == null)
                    continue;
                if (UnityEngine.Application.isPlaying)
                    Destroy(candidate);
                else
                    DestroyImmediate(candidate);
            }
            _retiredCandidates.Clear();
        }

        private void FillUnavailable()
        {
            SetText(titleText, "潜入密令：夺取密报");
            SetText(stateText, "状态：不可用");
            SetText(dateText, "日期：—");
            SetText(resourceText, "资金 —　物资 —");
            SetText(objectiveText, "缺少 GameSession，无法读取潜入状态。");
            SetText(teamText, "潜入队：—");
            SetText(statusText, "回合 —");
            SetText(guardText, "守卫：—");
            SetText(resultText, "结果：—");
            SetText(eventText, string.Empty);
            SetText(messageText, "缺少 GameSession，无法执行潜入操作。");
            SetText(chronicleText, string.Empty);
            SetText(selectionText, string.Empty);
            if (candidateScroll != null)
                candidateScroll.SetActive(false);
            SetInteractable(startButton, false);
            SetInteractable(northButton, false);
            SetInteractable(eastButton, false);
            SetInteractable(southButton, false);
            SetInteractable(westButton, false);
            SetInteractable(moveButton, false);
            SetInteractable(sprintButton, false);
            SetInteractable(waitButton, false);
            SetInteractable(interactButton, false);
            SetInteractable(extractButton, false);
            SetInteractable(withdrawButton, false);
            SetInteractable(switchButton, false);
            ClearCandidates();
        }

        private static string StateLabel(GameSession session, TacticalMissionSnapshot snapshot,
            bool completed)
        {
            if (snapshot != null)
            {
                switch (snapshot.Stage)
                {
                    case TacticalMissionStage.InProgress: return "潜入进行中";
                    case TacticalMissionStage.Succeeded: return "成功，已结算";
                    case TacticalMissionStage.Failed: return "失败，已结算";
                    case TacticalMissionStage.Withdrawn: return "撤退，已结算";
                }
            }
            if (completed)
                return "本次潜入已完成";
            return session.CurrentLocation == LocationId.Kiyosu
                ? "待出发（清洲本家）"
                : "待出发（需返回清洲本家）";
        }

        private string OperativeName(PersonId personId)
        {
            if (gameSession == null || personId.IsEmpty)
                return "—";
            SuccessionState succession = gameSession.Succession;
            return succession != null && succession.World != null
                ? IndustryService.PersonName(succession, personId)
                : personId.Value;
        }

        private GameDate CurrentDate() =>
            gameSession != null && gameSession.WorldClock != null
                ? gameSession.WorldClock.CurrentDate
                : default;

        private static int CellIndex(int x, int y)
        {
            if (x < 0 || x >= TacticalMissionScenario.Width ||
                y < 0 || y >= TacticalMissionScenario.Height)
                return -1;
            return y * TacticalMissionScenario.Width + x;
        }

        private static string Signed(int value) => (value >= 0 ? "+" : string.Empty) + value;

        private static string TextOf(Text text) => text != null ? text.text : string.Empty;

        private static void SetText(Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        /// <summary>
        /// Uniform scale that keeps the authored sheet inside a parent of the supplied size.
        /// Never above 1, because the authored layout is the reference. The CanvasScaler
        /// derives the overlay rect from the window aspect, so a 4:3 or an ultrawide window
        /// is a smaller parent, not a differently authored sheet.
        /// </summary>
        public static float FitScale(Vector2 availableSize, Vector2 pageSize)
        {
            if (availableSize.x <= 0f || availableSize.y <= 0f ||
                pageSize.x <= 0f || pageSize.y <= 0f)
                return 1f;
            float scale = Mathf.Min(availableSize.x / pageSize.x, availableSize.y / pageSize.y);
            return Mathf.Clamp(scale, MinimumPageScale, 1f);
        }

        /// <summary>
        /// True when the sheet fits the parent without dropping below
        /// <see cref="MinimumPageScale"/>. The generator gate uses this instead of comparing
        /// the authored size with the scaled parent at face value.
        /// </summary>
        public static bool FitsAtFloor(Vector2 availableSize, Vector2 pageSize) =>
            availableSize.x > 0f && availableSize.y > 0f && pageSize.x > 0f && pageSize.y > 0f &&
            Mathf.Min(availableSize.x / pageSize.x, availableSize.y / pageSize.y) >=
            MinimumPageScale;

        /// <summary>
        /// Keeps the fixed 1120x620 sheet inside the canvas-sized overlay it lives in: a
        /// uniform down-scale instead of clipping. At the 1280x720 reference the scale is 1,
        /// so the authored 14 px body type is the rendered size for every accepted layout. A
        /// narrower window is a runtime degradation, so the page shrinks rather than clips.
        /// The overlay stretches with the canvas, so this is also the resize path: the page
        /// re-fits while it stays open instead of keeping the scale of the old window.
        /// </summary>
        public void FitPageToParent()
        {
            if (panelRoot == null || !(panelRoot.transform is RectTransform page) ||
                !(page.parent is RectTransform parent))
                return;
            Vector2 available = parent.rect.size;
            Vector2 authored = page.rect.size;
            if (available.x <= 0f || available.y <= 0f ||
                authored.x <= 0f || authored.y <= 0f)
                return;
            _fittedParentSize = available;
            float scale = FitScale(available, authored);
            page.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// Raises the canvas-level overlay above its siblings. This is the z-order that makes
        /// the modal read above the town band, and it is re-applied on every open so a
        /// hand-reordered scene still renders the sheet on top.
        /// </summary>
        private void RaiseOverlay()
        {
            if (overlayRoot != null)
                overlayRoot.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Re-fits the sheet when the overlay changed size since the last fit. LateUpdate
        /// calls this every frame while the page is open, so a window or canvas resize keeps
        /// the modal contained instead of leaving the scale of the old window. Movement is
        /// only recomputed on a real dimension change.
        /// </summary>
        public void RefitIfParentResized()
        {
            if (panelRoot == null || !(panelRoot.transform is RectTransform page) ||
                !(page.parent is RectTransform parent))
                return;
            if ((parent.rect.size - _fittedParentSize).sqrMagnitude <= 0.01f)
                return;
            FitPageToParent();
        }

        private void LateUpdate()
        {
            if (_isOpen)
                RefitIfParentResized();
        }

        private void OnEnable()
        {
            if (openButton != null)
                openButton.gameObject.SetActive(!_isOpen);
            SetActive(page: _isOpen);
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            _isOpen = false;
            Unsubscribe();
            ClearCandidates();
            SetActive(page: false);
            if (openButton != null)
                openButton.gameObject.SetActive(true);
        }

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (gameSession != null && !_subscribed)
            {
                gameSession.SessionStateChanged += OnSessionStateChanged;
                _subscribed = true;
            }
            BindButton(openButton, OpenFromEntryButton, true);
            BindButton(closeButton, Close, true);
            BindButton(startButton, ClickStart, true);
            BindButton(northButton, ClickNorth, true);
            BindButton(eastButton, ClickEast, true);
            BindButton(southButton, ClickSouth, true);
            BindButton(westButton, ClickWest, true);
            BindButton(moveButton, ClickMove, true);
            BindButton(sprintButton, ClickSprint, true);
            BindButton(waitButton, ClickWait, true);
            BindButton(interactButton, ClickInteract, true);
            BindButton(extractButton, ClickExtract, true);
            BindButton(withdrawButton, ClickWithdraw, true);
            BindButton(switchButton, SwitchOperative, true);
        }

        private void Unsubscribe()
        {
            if (gameSession != null && _subscribed)
                gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
            BindButton(openButton, OpenFromEntryButton, false);
            BindButton(closeButton, Close, false);
            BindButton(startButton, ClickStart, false);
            BindButton(northButton, ClickNorth, false);
            BindButton(eastButton, ClickEast, false);
            BindButton(southButton, ClickSouth, false);
            BindButton(westButton, ClickWest, false);
            BindButton(moveButton, ClickMove, false);
            BindButton(sprintButton, ClickSprint, false);
            BindButton(waitButton, ClickWait, false);
            BindButton(interactButton, ClickInteract, false);
            BindButton(extractButton, ClickExtract, false);
            BindButton(withdrawButton, ClickWithdraw, false);
            BindButton(switchButton, SwitchOperative, false);
        }

        private void OnSessionStateChanged()
        {
            if (_isOpen)
                Refresh();
        }

        private static void BindButton(Button button, UnityAction action, bool add)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            if (add)
                button.onClick.AddListener(action);
        }
    }
}
