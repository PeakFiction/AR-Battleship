// =============================================================================
// CombatUI.cs
// Namespace: ARBattleship.Unity.UI
// =============================================================================
// PURPOSE:
//   Main singleplayer combat HUD for the AR gameplay scene.  Manages all
//   visual feedback during a match:
//     • 2D enemy grid overlay (toggleable via hamburger button)
//     • Minimap of the player's own board (auto-sized to a slot RectTransform)
//     • Enemy fleet indicator (5 coloured pips → grey as ships sink)
//     • Battle log popup with scrollable history
//     • Turn indicator (sprite or text, auto-hidden after N seconds)
//     • Pause overlay (freeze time, continue / abort mission)
//     • Game-over overlay (victory / defeat sprites, rematch / return)
//     • Top-right persistent turn status image
//     • Combat music integration (gameplay → danger → victory / loss)
//
// DESIGN PHILOSOPHY:
//   All UI panels that existed in the Figma design are assigned via Inspector
//   fields (Figma art objects).  Only dynamic elements (grid buttons, minimap
//   cells, fleet pips, log text, turn indicator) are generated at runtime
//   via the Build* and Bind* methods.  This keeps art control in the hands
//   of designers while keeping behaviour in code.
//
// EVENT WIRING:
//   Subscribes to GameManager static events in OnEnable / OnDisable:
//     OnGameStarted, OnPlayerShotFired, OnEnemyShotFired,
//     OnGameOver, OnBattleLogEntry, OnShipSunk
//
// COLOUR CONSTANTS (static readonly):
//   EmptyCellColor  – dark transparent grey  (#0000 26%)
//   HitColor        – red                    (#D91515)
//   MissColor       – blue                   (#3359D9)
//   ShipColor       – mid grey               (#8C8C8C)
//   EnemyFleetLiveColor – red               (#D91515)
//   EnemyFleetSunkColor – dark grey         (#383838)
//
// DEPENDENCIES:
//   • TextMeshPro (com.unity.textmeshpro)
//   • UnityEngine.UI (built-in)
//   • UnityEngine.SceneManagement (built-in)
//   • GameManager (project-level singleton)
//   • MusicManager (project-level singleton, optional)
// =============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Domain;

namespace ARBattleship.Unity.UI
{
    /// <summary>
    /// Singleplayer gameplay UI for AR mode.
    ///
    /// Inspector model:
    /// 1. The AR board remains the real gameplay surface.
    /// 2. The generated 2D enemy board is an optional toggle overlay.
    /// 3. The minimap fills a square RectTransform that you place in Canvas.
    /// 4. Figma art objects are assigned as static panels; only dynamic TMP text / pips are generated or assigned.
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        [Header("0 - Optional Turn Indicator Art")]
        [SerializeField] private Sprite yourTurnSprite;
        [SerializeField] private Sprite enemyTurnSprite;
        [SerializeField] private float turnIndicatorSeconds = 1.15f;

        [Header("1 - Enemy 2D Board Toggle Overlay")]
        [Tooltip("Your hamburger/menu button. This toggles the generated 2D enemy board on/off.")]
        [SerializeField] private Button enemyBoardToggleButton;

        [Tooltip("Parent RectTransform for the generated 2D enemy board. Create an empty UI object where you want the board to live. If empty, Canvas is used.")]
        [SerializeField] private RectTransform enemyBoardOverlayParent;

        [Tooltip("Show the 2D board immediately when gameplay starts. For AR gameplay this is usually false.")]
        [SerializeField] private bool showEnemyBoardOnGameStart = false;

        [Tooltip("Cell size for the generated 2D board. Increase this to make the overlay board bigger.")]
        [SerializeField] private float enemyBoardCellSize = 44f;

        [Tooltip("Position of the generated board relative to Enemy Board Overlay Parent.")]
        [SerializeField] private Vector2 enemyBoardAnchoredPosition = Vector2.zero;

        [Tooltip("Font size for A-J and 1-10 labels on the generated 2D board.")]
        [SerializeField] private int enemyBoardLabelFontSize = 16;

        [Header("2 - Minimap Slot")]
        [Tooltip("Square UI RectTransform that defines the minimap size and position. The minimap will fill this object.")]
        [SerializeField] private RectTransform minimapSlot;

        [Tooltip("Padding inside Minimap Slot. Use 0 if the slot already has the exact desired size.")]
        [SerializeField] private float minimapPadding = 0f;

        [Tooltip("Gap between minimap cells.")]
        [SerializeField] private float minimapCellGap = 1.5f;

        [Header("3 - Enemy Fleet Figma Panel")]
        [Tooltip("Your Figma Enemy Fleet component root. It should contain only the ENEMY FLEET art and the LOG button art.")]
        [SerializeField] private GameObject enemyFleetFigmaRoot;

        [Tooltip("The actual Button component for the LOG button inside the Figma Enemy Fleet component.")]
        [SerializeField] private Button enemyFleetLogButton;

        [Tooltip("Optional TMP text for '5 REMAINING'. Leave empty to generate it under Dynamic Root.")]
        [SerializeField] private TextMeshProUGUI enemyFleetRemainingText;

        [Tooltip("Parent for generated '5 REMAINING' text and red blocks. If empty, Enemy Fleet Figma Root is used.")]
        [SerializeField] private RectTransform enemyFleetDynamicRoot;

        [Tooltip("Optional parent for generated red fleet blocks. If empty, Dynamic Root is used.")]
        [SerializeField] private RectTransform enemyFleetPipsParent;

        [Tooltip("Optional sprite from your Canvas UI for each red block. If empty, solid Image blocks are generated.")]
        [SerializeField] private Sprite enemyFleetPipSprite;

        [Tooltip("Position for generated '5 REMAINING' if Remaining Text is not assigned.")]
        [SerializeField] private Vector2 generatedRemainingTextPosition = new Vector2(0f, -42f);

        [Tooltip("Size for generated '5 REMAINING' if Remaining Text is not assigned.")]
        [SerializeField] private Vector2 generatedRemainingTextSize = new Vector2(170f, 28f);

        [Tooltip("Top-left position for the generated fleet blocks.")]
        [SerializeField] private Vector2 generatedPipsStartPosition = new Vector2(0f, -72f);

        [Tooltip("Size of each generated fleet block.")]
        [SerializeField] private Vector2 generatedPipSize = new Vector2(16f, 16f);

        [Tooltip("Horizontal spacing between generated fleet blocks.")]
        [SerializeField] private float generatedPipSpacing = 22f;

        [Header("4 - Log Popup Figma Panel")]
        [Tooltip("Your full Figma Log popup root, including LOG title art, black box, border, and X button art.")]
        [SerializeField] private GameObject logPopupFigmaRoot;

        [Tooltip("The actual Button component for the X close button inside the Log popup.")]
        [SerializeField] private Button logPopupCloseButton;

        [Tooltip("TMP text placed inside your log box. Leave empty to generate scrollable text automatically.")]
        [SerializeField] private TextMeshProUGUI logPopupText;

        [Tooltip("RectTransform inside the black log box where generated scroll text should live. Leave empty to use Log Popup Root.")]
        [SerializeField] private RectTransform logPopupTextArea;

        [Tooltip("Padding used when the script generates a scrollable log text area.")]
        [SerializeField] private Vector4 generatedLogTextPadding = new Vector4(18f, 18f, 18f, 18f); // left, top, right, bottom

        [Header("5 - Turn Indicator Override")]
        [Tooltip("Optional Figma turn popup root. If empty, the script generates a simple full-width popup.")]
        [SerializeField] private GameObject turnIndicatorRoot;

        [Header("6 - Pause Overlay")]
        [Tooltip("The pause/menu button in the gameplay HUD, usually your hamburger button if it should open pause instead of toggling the 2D board.")]
        [SerializeField] private Button pauseButton;

        [Tooltip("Your full PauseOverlay root object. Assign the parent that contains DimBackground, PausePopupImage, ContinueInvisibleButton, and AbortMissionInvisibleButton.")]
        [SerializeField] private GameObject pauseOverlayRoot;

        [Tooltip("Invisible button over the CONTINUE area in your pause popup.")]
        [SerializeField] private Button pauseContinueButton;

        [Tooltip("Invisible button over the ABORT MISSION area in your pause popup.")]
        [SerializeField] private Button pauseAbortMissionButton;

        [Tooltip("Scene loaded when ABORT MISSION is pressed.")]
        [SerializeField] private string abortMissionSceneName = "1TitleScreen";

        [Tooltip("When true, setting Time.timeScale to 0 pauses AI delay coroutines and most gameplay timers while the pause overlay is open.")]
        [SerializeField] private bool freezeTimeWhilePaused = true;

        [Tooltip("Optional: close the log popup when opening pause.")]
        [SerializeField] private bool closeLogWhenPaused = true;

        [Header("7 - Game Over Overlay")]
        [Tooltip("Your full GameOverOverlay root object. Assign the parent that contains DimBackground, GameOverPopupImage, RematchButton, and ReturnButton.")]
        [SerializeField] private GameObject gameOverOverlayRoot;

        [Tooltip("Image component on the game-over BOX component. This should be the foreground black/white popup box with the buttons.")]
        [SerializeField] private Image gameOverPopupImage;

        [Tooltip("Image component on the separate background/backing art behind the game-over box, such as the repeated VICTORY or COMPROMISE component.")]
        [SerializeField] private Image gameOverBackingImage;

        [Tooltip("Foreground box sprite shown when the player wins. Optional if your box art is already static.")]
        [SerializeField] private Sprite victoryPopupSprite;

        [Tooltip("Foreground box sprite shown when the player loses. Optional if your box art is already static.")]
        [SerializeField] private Sprite lossPopupSprite;

        [Tooltip("Backing/background sprite shown behind the box when the player wins, such as the blue VICTORY art.")]
        [SerializeField] private Sprite victoryBackingSprite;

        [Tooltip("Backing/background sprite shown behind the box when the player loses, such as the red COMPROMISE art.")]
        [SerializeField] private Sprite lossBackingSprite;

        [Tooltip("Button component on RematchButton.")]
        [SerializeField] private Button gameOverRematchButton;

        [Tooltip("Button component on ReturnButton.")]
        [SerializeField] private Button gameOverReturnButton;

        [Tooltip("Reload the active scene for rematch. This is safest when setup and gameplay are in the same scene.")]
        [SerializeField] private bool rematchReloadsCurrentScene = true;

        [Tooltip("Optional scene name for rematch when Rematch Reloads Current Scene is false. Leave empty to use ResetGame only.")]
        [SerializeField] private string rematchSceneName = "";

        [Tooltip("Scene loaded when Return is pressed.")]
        [SerializeField] private string returnSceneName = "1TitleScreen";

        [Tooltip("Set Time.timeScale to 0 while the game-over overlay is open.")]
        [SerializeField] private bool freezeTimeOnGameOver = true;

        [Tooltip("In this project player index 0 is the human player. Change only if your GameManager uses another index.")]
        [SerializeField] private int localPlayerIndex = 0;

        [Header("8.5 - Combat Music")]
        [Tooltip("Play gameplay music when combat starts, danger music when your own ship is hit, and victory/loss music at game over.")]
        [SerializeField] private bool controlCombatMusic = true;

        [Tooltip("Switch to danger music when the enemy hits one of your ships. Misses do not change the music.")]
        [SerializeField] private bool switchToDangerMusicWhenPlayerShipHit = true;

        [Header("8 - Top Right Turn Status Image")]
        [Tooltip("Persistent HUD image in the top-right corner. This is separate from the temporary turn popup.")]
        [SerializeField] private Image topRightTurnStatusImage;

        [Tooltip("Sprite shown in the top-right HUD while it is the player's turn.")]
        [SerializeField] private Sprite topRightYourTurnSprite;

        [Tooltip("Sprite shown in the top-right HUD while it is the enemy's turn.")]
        [SerializeField] private Sprite topRightEnemyTurnSprite;

        [Tooltip("Hide the top-right turn status image until gameplay starts.")]
        [SerializeField] private bool hideTopRightTurnStatusUntilGameStarts = true;

        [Tooltip("Image inside Turn Indicator Root. Sprites above are swapped into this image.")]
        [SerializeField] private Image turnIndicatorImage;

        [Tooltip("Fallback TMP text used if no sprite is assigned for the active turn.")]
        [SerializeField] private TextMeshProUGUI turnIndicatorText;

        private GameObject combatPanel;
        private GameObject minimapPanel;
        private GameObject enemyFleetPanel;
        private GameObject logPopupPanel;
        private GameObject turnIndicatorPanel;

        private readonly Button[][] enemyGridButtons = new Button[10][];
        private readonly Image[][] minimapCells = new Image[10][];
        private readonly Image[] generatedEnemyFleetPips = new Image[5];
        private readonly List<string> battleLogEntries = new List<string>();

        private int remainingEnemyShips = 5;
        private bool enemyBoardVisible;
        private bool isPaused;
        private bool isGameOver;
        private bool isLocalPlayerTurn;
        private bool playerShipHasBeenHit;

        // Some singleplayer builds emit ShipSunkEvent when a ship is sunk, but others only return
        // ShotOutcome.Sunk from FireShot/OnPlayerShotFired. This prevents the enemy fleet UI from
        // getting stuck, while also preventing double-decrement when both signals are present.
        private bool enemyShipSunkEventReceivedForLastPlayerShot;
        private Coroutine turnIndicatorRoutine;

        private static readonly Color EmptyCellColor = new Color(0f, 0f, 0f, 0.15f);
        private static readonly Color GridLineColor = Color.white;
        private static readonly Color HitColor = new Color(0.85f, 0.08f, 0.08f, 1f);
        private static readonly Color MissColor = new Color(0.20f, 0.35f, 0.85f, 1f);
        private static readonly Color ShipColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color EnemyFleetLiveColor = new Color(0.85f, 0.08f, 0.08f, 1f);
        private static readonly Color EnemyFleetSunkColor = new Color(0.22f, 0.22f, 0.22f, 1f);

        private void OnEnable()
        {
            GameManager.OnGameStarted += HandleGameStarted;
            GameManager.OnPlayerShotFired += HandlePlayerShot;
            GameManager.OnEnemyShotFired += HandleEnemyShot;
            GameManager.OnGameOver += HandleGameOver;
            GameManager.OnBattleLogEntry += HandleBattleLogEntry;
            GameManager.OnShipSunk += HandleShipSunk;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= HandleGameStarted;
            GameManager.OnPlayerShotFired -= HandlePlayerShot;
            GameManager.OnEnemyShotFired -= HandleEnemyShot;
            GameManager.OnGameOver -= HandleGameOver;
            GameManager.OnBattleLogEntry -= HandleBattleLogEntry;
            GameManager.OnShipSunk -= HandleShipSunk;
            if ((isPaused && freezeTimeWhilePaused) || (isGameOver && freezeTimeOnGameOver))
                Time.timeScale = 1f;
        }

        private void Start()
        {
            BuildCombatUI();
            BuildMinimapUI();
            BindEnemyFleetUI();
            BindLogPopupUI();
            BindTurnIndicatorUI();

            if (enemyBoardToggleButton != null)
            {
                enemyBoardToggleButton.onClick.RemoveListener(ToggleEnemyBoard);
                enemyBoardToggleButton.onClick.AddListener(ToggleEnemyBoard);
            }

            BindPauseOverlayUI();
            BindGameOverOverlayUI();

            combatPanel.SetActive(false);
            minimapPanel.SetActive(false);
            if (enemyFleetPanel != null) enemyFleetPanel.SetActive(false);
            if (logPopupPanel != null) logPopupPanel.SetActive(false);
            if (turnIndicatorPanel != null) turnIndicatorPanel.SetActive(false);
            if (pauseOverlayRoot != null) pauseOverlayRoot.SetActive(false);
            if (gameOverOverlayRoot != null) gameOverOverlayRoot.SetActive(false);

            if (topRightTurnStatusImage != null && hideTopRightTurnStatusUntilGameStarts)
                topRightTurnStatusImage.enabled = false;

            SetLocalTurn(false, false);
        }

        private void HandleGameStarted()
        {
            SetGameOverOverlay(false, false);
            SetPaused(false);
            remainingEnemyShips = 5;
            playerShipHasBeenHit = false;
            enemyShipSunkEventReceivedForLastPlayerShot = false;
            PlayGameplayMusic();
            battleLogEntries.Clear();
            RefreshEnemyFleetIndicator();
            RefreshLogText();

            enemyBoardVisible = showEnemyBoardOnGameStart;
            combatPanel.SetActive(enemyBoardVisible);
            minimapPanel.SetActive(true);
            if (enemyFleetPanel != null) enemyFleetPanel.SetActive(true);
            if (logPopupPanel != null) logPopupPanel.SetActive(false);

            ResetEnemyGridVisuals();
            ResetMinimapVisuals();
            RefreshMinimap();
            SetLocalTurn(true, true);
        }

        private void HandlePlayerShot(int x, int y, ShotOutcome outcome, int? hitSegmentIndex, string? shipOrientation, string? shipType)
        {
            if (IsInsideGrid(x, y) && enemyGridButtons[x][y] != null)
            {
                Color color = outcome == ShotOutcome.Miss ? MissColor : HitColor;
                enemyGridButtons[x][y].GetComponent<Image>().color = color;
                enemyGridButtons[x][y].interactable = false;
            }

            if (outcome == ShotOutcome.Sunk)
            {
                // Fallback path: in some builds, OnPlayerShotFired is the only reliable sunk signal.
                // If HandleShipSunk already handled this shot, this call is skipped.
                if (!enemyShipSunkEventReceivedForLastPlayerShot)
                    MarkEnemyFleetShipSunk();

                enemyShipSunkEventReceivedForLastPlayerShot = false;
            }
            else
            {
                enemyShipSunkEventReceivedForLastPlayerShot = false;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.InProgress)
                SetLocalTurn(false, true);
        }

        private void HandleEnemyShot(int x, int y, ShotOutcome outcome, int? hitSegmentIndex, string? shipOrientation, string? shipType)
        {
            Debug.Log($"[CombatUI] HandleEnemyShot called: ({x},{y}) -> {outcome}");
            
            if (IsInsideGrid(x, y) && minimapCells[x][y] != null)
                minimapCells[x][y].color = outcome == ShotOutcome.Miss ? MissColor : HitColor;

            if (outcome != ShotOutcome.Miss)
                PlayDangerMusicIfPlayerShipHit();

            Debug.Log($"[CombatUI] Current phase: {GameManager.Instance?.CurrentPhase}, Setting turn back to player");
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.InProgress)
                SetLocalTurn(true, true);
        }

        private void HandleGameOver(int winnerIndex)
        {
            SetLocalTurn(false, false);
            combatPanel.SetActive(false);
            if (turnIndicatorRoutine != null)
            {
                StopCoroutine(turnIndicatorRoutine);
                turnIndicatorRoutine = null;
            }

            if (turnIndicatorPanel != null) turnIndicatorPanel.SetActive(false);
            if (logPopupPanel != null) logPopupPanel.SetActive(false);
            if (pauseOverlayRoot != null) pauseOverlayRoot.SetActive(false);
            if (topRightTurnStatusImage != null)
                topRightTurnStatusImage.enabled = false;

            bool playerWon = winnerIndex == localPlayerIndex;
            PlayGameOverMusic(playerWon);
            SetGameOverOverlay(true, playerWon);
        }

        private void HandleBattleLogEntry(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry)) return;

            battleLogEntries.Insert(0, entry);
            if (battleLogEntries.Count > 80)
                battleLogEntries.RemoveAt(battleLogEntries.Count - 1);

            RefreshLogText();
        }

        private void HandleShipSunk(int playerIndex, string shipType)
        {
            // Player index 1 is the AI/enemy board in the current singleplayer project.
            // This event is the preferred path because it includes the sunk ship type.
            if (playerIndex != 1) return;

            enemyShipSunkEventReceivedForLastPlayerShot = true;
            MarkEnemyFleetShipSunk();
        }

        private void MarkEnemyFleetShipSunk()
        {
            remainingEnemyShips = Mathf.Max(0, remainingEnemyShips - 1);
            RefreshEnemyFleetIndicator();
        }

        private void PlayGameplayMusic()
        {
            if (!controlCombatMusic || global::MusicManager.Instance == null)
                return;

            global::MusicManager.Instance.PlayGameplayMusic();
        }

        private void PlayDangerMusicIfPlayerShipHit()
        {
            if (!controlCombatMusic || !switchToDangerMusicWhenPlayerShipHit)
                return;

            if (playerShipHasBeenHit)
                return;

            playerShipHasBeenHit = true;

            if (global::MusicManager.Instance != null)
                global::MusicManager.Instance.PlayDangerMusic();
        }

        private void PlayGameOverMusic(bool playerWon)
        {
            if (!controlCombatMusic || global::MusicManager.Instance == null)
                return;

            if (playerWon)
                global::MusicManager.Instance.PlayVictoryMusic();
            else
                global::MusicManager.Instance.PlayLossMusic();
        }

        private void OnCellClicked(int x, int y)
        {
            if (isPaused || isGameOver) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentPhase != GamePhase.InProgress) return;

            if (!isLocalPlayerTurn || GameManager.Instance.CurrentPlayerTurn != 0)
            {
                Debug.Log("It is not your turn yet.");
                return;
            }

            ShotOutcome outcome = GameManager.Instance.FireShot(x, y);
            Debug.Log($"Fired at ({x},{y}): {outcome}");
        }

        private void SetLocalTurn(bool isPlayerTurn, bool showIndicator)
        {
            isLocalPlayerTurn = isPlayerTurn;
            SetEnemyGridInteractable(isPlayerTurn);
            UpdateTopRightTurnStatus(isPlayerTurn);

            if (showIndicator)
                ShowTurnIndicator(isPlayerTurn);
        }

        private void UpdateTopRightTurnStatus(bool isPlayerTurn)
        {
            if (topRightTurnStatusImage == null)
                return;

            Sprite sprite = isPlayerTurn ? topRightYourTurnSprite : topRightEnemyTurnSprite;

            topRightTurnStatusImage.sprite = sprite;
            topRightTurnStatusImage.enabled = sprite != null;
            topRightTurnStatusImage.preserveAspect = true;
        }

        private void SetEnemyGridInteractable(bool interactable)
        {
            for (int x = 0; x < 10; x++)
            {
                if (enemyGridButtons[x] == null)
                    continue;

                for (int y = 0; y < 10; y++)
                {
                    if (enemyGridButtons[x][y] == null)
                        continue;

                    Image img = enemyGridButtons[x][y].GetComponent<Image>();
                    bool alreadyShot = img != null && (img.color == HitColor || img.color == MissColor);

                    enemyGridButtons[x][y].interactable =
                        interactable &&
                        !isPaused &&
                        !isGameOver &&
                        !alreadyShot;
                }
            }
        }

        private void RefreshMinimap()
        {
            if (GameManager.Instance == null) return;

            var snapshot = GameManager.Instance.GetSnapshot();
            foreach (var cell in snapshot.PlayerOne.Cells)
            {
                if (cell.State == CellViewState.Ship && IsInsideGrid(cell.X, cell.Y))
                    minimapCells[cell.X][cell.Y].color = ShipColor;
            }
        }

        private void ShowTurnIndicator(bool isPlayerTurn)
        {
            if (turnIndicatorPanel == null) return;

            if (turnIndicatorRoutine != null)
                StopCoroutine(turnIndicatorRoutine);

            Sprite sprite = isPlayerTurn ? yourTurnSprite : enemyTurnSprite;

            if (turnIndicatorImage != null)
            {
                turnIndicatorImage.sprite = sprite;
                turnIndicatorImage.enabled = sprite != null;
                turnIndicatorImage.preserveAspect = true;
            }

            if (turnIndicatorText != null)
            {
                turnIndicatorText.text = isPlayerTurn ? "YOUR TURN" : "ENEMY TURN";
                turnIndicatorText.gameObject.SetActive(sprite == null);
            }

            turnIndicatorRoutine = StartCoroutine(ShowTurnIndicatorRoutine());
        }

        private IEnumerator ShowTurnIndicatorRoutine()
        {
            turnIndicatorPanel.SetActive(true);
            yield return new WaitForSeconds(turnIndicatorSeconds);
            turnIndicatorPanel.SetActive(false);
            turnIndicatorRoutine = null;
        }

        private void ToggleEnemyBoard()
        {
            enemyBoardVisible = !enemyBoardVisible;
            if (combatPanel != null)
                combatPanel.SetActive(enemyBoardVisible);
        }

        private void ToggleLogPopup()
        {
            if (logPopupPanel != null)
                logPopupPanel.SetActive(!logPopupPanel.activeSelf);
        }

        private void RefreshEnemyFleetIndicator()
        {
            if (enemyFleetRemainingText != null)
                enemyFleetRemainingText.text = $"{remainingEnemyShips} REMAINING";

            for (int i = 0; i < generatedEnemyFleetPips.Length; i++)
            {
                if (generatedEnemyFleetPips[i] == null) continue;
                generatedEnemyFleetPips[i].color = i < remainingEnemyShips ? EnemyFleetLiveColor : EnemyFleetSunkColor;
            }
        }

        private void RefreshLogText()
        {
            if (logPopupText == null) return;
            logPopupText.text = string.Join("\n\n", battleLogEntries);
        }

        private void ResetEnemyGridVisuals()
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (enemyGridButtons[x][y] == null) continue;
                    Image img = enemyGridButtons[x][y].GetComponent<Image>();
                    img.color = EmptyCellColor;
                    enemyGridButtons[x][y].interactable = true;
                }
            }
        }

        private void ResetMinimapVisuals()
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    if (minimapCells[x][y] != null)
                        minimapCells[x][y].color = EmptyCellColor;
                }
            }
        }

        private void BindPauseOverlayUI()
        {
            if (pauseOverlayRoot != null)
                pauseOverlayRoot.SetActive(false);

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OpenPauseOverlay);
                pauseButton.onClick.AddListener(OpenPauseOverlay);
            }

            if (pauseContinueButton != null)
            {
                pauseContinueButton.onClick.RemoveListener(ClosePauseOverlay);
                pauseContinueButton.onClick.AddListener(ClosePauseOverlay);
            }

            if (pauseAbortMissionButton != null)
            {
                pauseAbortMissionButton.onClick.RemoveListener(AbortMission);
                pauseAbortMissionButton.onClick.AddListener(AbortMission);
            }
        }

        private void OpenPauseOverlay()
        {
            if (isGameOver) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase != GamePhase.InProgress)
                return;

            if (closeLogWhenPaused && logPopupPanel != null)
                logPopupPanel.SetActive(false);

            SetPaused(true);
        }

        private void ClosePauseOverlay()
        {
            SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused;

            if (pauseOverlayRoot != null)
                pauseOverlayRoot.SetActive(paused);

            SetEnemyGridInteractable(isLocalPlayerTurn && !paused);

            if (freezeTimeWhilePaused)
            {
                if (paused)
                    Time.timeScale = 0f;
                else if (!isGameOver)
                    Time.timeScale = 1f;
            }
        }

        private void AbortMission()
        {
            SetPaused(false);

            if (GameManager.Instance != null)
                GameManager.Instance.ResetGame();

            if (!string.IsNullOrWhiteSpace(abortMissionSceneName))
                SceneManager.LoadScene(abortMissionSceneName);
        }

        private void BindGameOverOverlayUI()
        {
            if (gameOverOverlayRoot != null)
                gameOverOverlayRoot.SetActive(false);

            if (gameOverRematchButton != null)
            {
                gameOverRematchButton.onClick.RemoveListener(RematchGame);
                gameOverRematchButton.onClick.AddListener(RematchGame);
            }

            if (gameOverReturnButton != null)
            {
                gameOverReturnButton.onClick.RemoveListener(ReturnFromGameOver);
                gameOverReturnButton.onClick.AddListener(ReturnFromGameOver);
            }
        }

        private void SetGameOverOverlay(bool visible, bool playerWon)
        {
            isGameOver = visible;

            if (gameOverOverlayRoot != null)
                gameOverOverlayRoot.SetActive(visible);

            SetEnemyGridInteractable(isLocalPlayerTurn && !visible);

            if (visible)
            {
                ApplyGameOverVisuals(playerWon);
            }

            if (freezeTimeOnGameOver)
                Time.timeScale = visible ? 0f : 1f;
        }

        private void ApplyGameOverVisuals(bool playerWon)
        {
            if (gameOverPopupImage != null)
            {
                Sprite boxSprite = playerWon ? victoryPopupSprite : lossPopupSprite;

                if (boxSprite != null)
                    gameOverPopupImage.sprite = boxSprite;

                gameOverPopupImage.enabled = gameOverPopupImage.sprite != null;
                gameOverPopupImage.preserveAspect = true;
            }

            if (gameOverBackingImage != null)
            {
                Sprite backingSprite = playerWon ? victoryBackingSprite : lossBackingSprite;

                if (backingSprite != null)
                    gameOverBackingImage.sprite = backingSprite;

                gameOverBackingImage.enabled = gameOverBackingImage.sprite != null;
                gameOverBackingImage.preserveAspect = true;
                gameOverBackingImage.raycastTarget = false;
            }
        }

        private void RematchGame()
        {
            SetGameOverOverlay(false, false);
            SetPaused(false);

            if (GameManager.Instance != null)
                GameManager.Instance.ResetGame();

            if (rematchReloadsCurrentScene)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(activeScene.name);
                return;
            }

            if (!string.IsNullOrWhiteSpace(rematchSceneName))
                SceneManager.LoadScene(rematchSceneName);
        }

        private void ReturnFromGameOver()
        {
            SetGameOverOverlay(false, false);
            SetPaused(false);

            if (GameManager.Instance != null)
                GameManager.Instance.ResetGame();

            if (!string.IsNullOrWhiteSpace(returnSceneName))
                SceneManager.LoadScene(returnSceneName);
        }

        private void BuildCombatUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = enemyBoardOverlayParent != null ? enemyBoardOverlayParent : canvas.transform;

            combatPanel = new GameObject("Generated_Enemy2DBoard_Overlay");
            combatPanel.transform.SetParent(parent, false);
            RectTransform rt = combatPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = enemyBoardAnchoredPosition;
            rt.sizeDelta = new Vector2(enemyBoardCellSize * 11f, enemyBoardCellSize * 11f);

            float startX = -enemyBoardCellSize * 4.5f;
            float startY = enemyBoardCellSize * 4.5f + 10f;

            BuildGridLabels(combatPanel.transform, startX, startY, enemyBoardCellSize, enemyBoardLabelFontSize);

            for (int x = 0; x < 10; x++)
            {
                enemyGridButtons[x] = new Button[10];
                for (int y = 0; y < 10; y++)
                {
                    int cx = x, cy = y;
                    float px = startX + x * enemyBoardCellSize;
                    float py = startY - y * enemyBoardCellSize;
                    GameObject cell = CreateButton(combatPanel.transform, $"EnemyCell_{x}_{y}", new Vector2(px, py), new Vector2(enemyBoardCellSize - 4, enemyBoardCellSize - 4));
                    enemyGridButtons[x][y] = cell.GetComponent<Button>();
                    enemyGridButtons[x][y].onClick.AddListener(() => OnCellClicked(cx, cy));
                }
            }
        }

        private void BuildGridLabels(Transform parent, float startX, float startY, float cellSize, int fontSize)
        {
            for (int x = 0; x < 10; x++)
                CreateTMP(parent, (x + 1).ToString(), new Vector2(startX + x * cellSize, startY + cellSize * 0.67f), new Vector2(cellSize, 20), fontSize, TextAlignmentOptions.Center);

            for (int y = 0; y < 10; y++)
                CreateTMP(parent, ((char)('A' + y)).ToString(), new Vector2(startX - cellSize * 0.72f, startY - y * cellSize), new Vector2(24, cellSize), fontSize, TextAlignmentOptions.Center);
        }

        private void BuildMinimapUI()
        {
            Canvas.ForceUpdateCanvases();
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = minimapSlot != null ? minimapSlot : canvas.transform;

            minimapPanel = new GameObject("Generated_Minimap_Fills_MinimapSlot");
            minimapPanel.transform.SetParent(parent, false);
            RectTransform panelRT = minimapPanel.AddComponent<RectTransform>();

            if (minimapSlot != null)
            {
                panelRT.anchorMin = Vector2.zero;
                panelRT.anchorMax = Vector2.one;
                panelRT.pivot = new Vector2(0.5f, 0.5f);
                panelRT.offsetMin = new Vector2(minimapPadding, minimapPadding);
                panelRT.offsetMax = new Vector2(-minimapPadding, -minimapPadding);
            }
            else
            {
                panelRT.anchorMin = new Vector2(0f, 0f);
                panelRT.anchorMax = new Vector2(0f, 0f);
                panelRT.pivot = new Vector2(0f, 0f);
                panelRT.anchoredPosition = new Vector2(18f, 18f);
                panelRT.sizeDelta = new Vector2(170f, 170f);
            }

            Image bg = minimapPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.1f);
            bg.raycastTarget = false;

            Vector2 squareSize = GetRectSize(minimapSlot != null ? minimapSlot : panelRT);
            float side = Mathf.Max(1f, Mathf.Min(squareSize.x, squareSize.y) - minimapPadding * 2f);
            float cellSize = side / 10f;

            for (int x = 0; x < 10; x++)
            {
                minimapCells[x] = new Image[10];
                for (int y = 0; y < 10; y++)
                {
                    GameObject cell = new GameObject($"Mini_{x}_{y}");
                    cell.transform.SetParent(minimapPanel.transform, false);
                    RectTransform cellRT = cell.AddComponent<RectTransform>();
                    cellRT.anchorMin = new Vector2(0f, 0f);
                    cellRT.anchorMax = new Vector2(0f, 0f);
                    cellRT.pivot = new Vector2(0f, 0f);
                    cellRT.anchoredPosition = new Vector2(x * cellSize, (9 - y) * cellSize);
                    cellRT.sizeDelta = new Vector2(Mathf.Max(1f, cellSize - minimapCellGap), Mathf.Max(1f, cellSize - minimapCellGap));

                    Image img = cell.AddComponent<Image>();
                    img.color = EmptyCellColor;
                    img.raycastTarget = false;
                    minimapCells[x][y] = img;

                    AddOutline(cell, GridLineColor, new Vector2(1f, -1f));
                }
            }
        }

        private void BindEnemyFleetUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (enemyFleetFigmaRoot != null)
            {
                enemyFleetPanel = enemyFleetFigmaRoot;
            }
            else
            {
                enemyFleetPanel = new GameObject("Generated_EnemyFleetPanel_NoFigmaAssigned");
                enemyFleetPanel.transform.SetParent(canvas.transform, false);
                RectTransform rt = enemyFleetPanel.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = new Vector2(-18, 18);
                rt.sizeDelta = new Vector2(210, 120);
                Image bg = enemyFleetPanel.AddComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.1f);
                bg.raycastTarget = false;
                CreateTMP(enemyFleetPanel.transform, "ENEMY FLEET", new Vector2(0, -8), new Vector2(210, 36), 24, TextAlignmentOptions.Left, true);
            }

            if (enemyFleetLogButton != null)
            {
                enemyFleetLogButton.onClick.RemoveListener(ToggleLogPopup);
                enemyFleetLogButton.onClick.AddListener(ToggleLogPopup);
            }

            RectTransform dynamicRoot = enemyFleetDynamicRoot;
            if (dynamicRoot == null && enemyFleetPanel != null)
                dynamicRoot = enemyFleetPanel.GetComponent<RectTransform>();

            if (enemyFleetRemainingText == null && dynamicRoot != null)
            {
                enemyFleetRemainingText = CreateTMP(dynamicRoot, "5 REMAINING", generatedRemainingTextPosition, generatedRemainingTextSize, 18, TextAlignmentOptions.Left, false);
            }

            RectTransform pipsParent = enemyFleetPipsParent != null ? enemyFleetPipsParent : dynamicRoot;
            if (pipsParent != null)
            {
                for (int i = 0; i < generatedEnemyFleetPips.Length; i++)
                {
                    GameObject pip = new GameObject("Generated_EnemyFleetBlock_" + i);
                    pip.transform.SetParent(pipsParent, false);
                    RectTransform pipRT = pip.AddComponent<RectTransform>();
                    pipRT.anchorMin = new Vector2(0f, 1f);
                    pipRT.anchorMax = new Vector2(0f, 1f);
                    pipRT.pivot = new Vector2(0f, 1f);
                    pipRT.anchoredPosition = new Vector2(generatedPipsStartPosition.x + i * generatedPipSpacing, generatedPipsStartPosition.y);
                    pipRT.sizeDelta = generatedPipSize;

                    Image img = pip.AddComponent<Image>();
                    img.sprite = enemyFleetPipSprite;
                    img.color = EnemyFleetLiveColor;
                    img.raycastTarget = false;
                    generatedEnemyFleetPips[i] = img;
                }
            }
        }

        private void BindLogPopupUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (logPopupFigmaRoot != null)
            {
                logPopupPanel = logPopupFigmaRoot;
            }
            else
            {
                logPopupPanel = new GameObject("Generated_LogPopup_NoFigmaAssigned");
                logPopupPanel.transform.SetParent(canvas.transform, false);
                RectTransform panelRT = logPopupPanel.AddComponent<RectTransform>();
                panelRT.anchorMin = new Vector2(1, 0);
                panelRT.anchorMax = new Vector2(1, 0);
                panelRT.pivot = new Vector2(1, 0);
                panelRT.anchoredPosition = new Vector2(-18, 115);
                panelRT.sizeDelta = new Vector2(300, 420);
                Image panelBg = logPopupPanel.AddComponent<Image>();
                panelBg.color = new Color(0, 0, 0, 0.92f);
                AddOutline(logPopupPanel, Color.white, new Vector2(1.5f, -1.5f));
                CreateTMP(logPopupPanel.transform, "LOG", new Vector2(16, 62), new Vector2(200, 72), 48, TextAlignmentOptions.Left, true);
            }

            if (logPopupCloseButton != null)
            {
                logPopupCloseButton.onClick.RemoveAllListeners();
                logPopupCloseButton.onClick.AddListener(() => logPopupPanel.SetActive(false));
            }

            if (logPopupText == null)
            {
                RectTransform textArea = logPopupTextArea != null ? logPopupTextArea : logPopupPanel.GetComponent<RectTransform>();
                logPopupText = CreateGeneratedScrollableLog(textArea);
            }
        }

        private TextMeshProUGUI CreateGeneratedScrollableLog(RectTransform parent)
        {
            GameObject viewport = new GameObject("Generated_LogViewport");
            viewport.transform.SetParent(parent, false);
            RectTransform viewportRT = viewport.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(generatedLogTextPadding.x, generatedLogTextPadding.w);
            viewportRT.offsetMax = new Vector2(-generatedLogTextPadding.z, -generatedLogTextPadding.y);

            Image viewportMaskImage = viewport.AddComponent<Image>();
            viewportMaskImage.color = new Color(0, 0, 0, 0.01f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = new GameObject("Generated_LogContent");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 1200);

            TextMeshProUGUI text = content.AddComponent<TextMeshProUGUI>();
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopRight;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            text.text = "";

            ScrollRect scroll = parent.gameObject.GetComponent<ScrollRect>();
            if (scroll == null) scroll = parent.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRT;
            scroll.content = contentRT;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;

            return text;
        }

        private void BindTurnIndicatorUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (turnIndicatorRoot != null)
            {
                turnIndicatorPanel = turnIndicatorRoot;
                if (turnIndicatorImage != null)
                    turnIndicatorImage.preserveAspect = true;
                return;
            }

            turnIndicatorPanel = new GameObject("Generated_TurnIndicator");
            turnIndicatorPanel.transform.SetParent(canvas.transform, false);
            RectTransform rt = turnIndicatorPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 110f);

            Image bg = turnIndicatorPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.18f);
            bg.raycastTarget = false;

            GameObject imageObj = new GameObject("Generated_TurnIndicatorImage");
            imageObj.transform.SetParent(turnIndicatorPanel.transform, false);
            RectTransform imageRT = imageObj.AddComponent<RectTransform>();
            imageRT.anchorMin = Vector2.zero;
            imageRT.anchorMax = Vector2.one;
            imageRT.offsetMin = new Vector2(-30, -10);
            imageRT.offsetMax = new Vector2(30, 10);
            turnIndicatorImage = imageObj.AddComponent<Image>();
            turnIndicatorImage.preserveAspect = true;
            turnIndicatorImage.raycastTarget = false;

            turnIndicatorText = CreateTMP(turnIndicatorPanel.transform, "YOUR TURN", Vector2.zero, new Vector2(900, 90), 54, TextAlignmentOptions.Center, true);
        }

        private GameObject CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(string.IsNullOrEmpty(label) ? "Button" : label);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image image = obj.AddComponent<Image>();
            image.color = label.StartsWith("EnemyCell") ? EmptyCellColor : new Color(0, 0, 0, 0.65f);
            Button button = obj.AddComponent<Button>();
            AddOutline(obj, GridLineColor, new Vector2(1f, -1f));

            return obj;
        }

        private TextMeshProUGUI CreateTMP(Transform parent, string value, Vector2 anchoredPos, Vector2 size, int fontSize, TextAlignmentOptions alignment, bool bold = false)
        {
            GameObject obj = new GameObject("TMP_" + value);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.raycastTarget = false;
            return text;
        }

        private void AddOutline(GameObject obj, Color color, Vector2 effectDistance)
        {
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = effectDistance;
        }

        private static Vector2 GetRectSize(RectTransform rt)
        {
            if (rt == null) return new Vector2(170f, 170f);
            Vector2 size = rt.rect.size;
            if (size.x <= 0.01f || size.y <= 0.01f)
                size = rt.sizeDelta;
            if (size.x <= 0.01f) size.x = 170f;
            if (size.y <= 0.01f) size.y = size.x;
            return size;
        }

        private static bool IsInsideGrid(int x, int y)
        {
            return x >= 0 && x < 10 && y >= 0 && y < 10;
        }
    }
}