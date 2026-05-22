using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Domain;
using ARBattleship.Multiplayer.Battleship;

namespace ARBattleship.Unity.UI
{
    /// <summary>
    /// Manages the multiplayer ship-placement screen, including grid painting, drag placement, server validation, and start-game requests.
    /// </summary>
    public sealed class MultiplayerPlacementUI : MonoBehaviour
    {
        [System.Serializable]
        /// <summary>
        /// Inspector data for one selectable ship, including its button and ship-specific UI sprites.
        /// </summary>
        public class ShipUIOption
        {
            public string shipType;
            public Button button;

            [Header("Figma Selected Ship Images")]
            public Sprite nameSprite;
            public Sprite previewSprite;
            public Sprite backingTextSprite;

            [Header("Placement Popup - Split Visuals")]
            [Tooltip("Assign the ship-specific backing art: ship name, ship silhouette, length text, etc.")]
            public Sprite popupBackingSprite;

            [Tooltip("Optional. Assign the ship-specific button box art. If empty, the existing Popup Button Box Image sprite is kept.")]
            public Sprite popupButtonBoxSprite;
        }
 
        /// <summary>
        /// Lightweight local representation of a ship placement before and after server confirmation.
        /// </summary>
        private class PlacedShip
        {
            public string Type;
            public int X;
            public int Y;
            public Orientation Orientation;

            public PlacedShip(string type, int x, int y, Orientation orientation)
            {
                Type = type;
                X = x;
                Y = y;
                Orientation = orientation;
            }
        }

        [Header("Multiplayer")]
        [SerializeField] private MultiplayerBattleshipSession session;

        [Header("Ship Options")]
        [SerializeField] private ShipUIOption[] ships;

        [Header("Selected Ship Display Images")]
        [SerializeField] private Image selectedShipNameImage;
        [SerializeField] private Image selectedShipPreviewImage;
        [SerializeField] private Image selectedShipBackingTextImage;

        [Header("Grid")]
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private Button gridCellPrefab;
        [SerializeField] private Vector2 cellSize = new Vector2(78f, 78f);
        [SerializeField] private Vector2 cellSpacing = new Vector2(24f, 24f);

        [Header("Grid Labels")]
        [SerializeField] private RectTransform columnLabelRoot;
        [SerializeField] private RectTransform rowLabelRoot;
        [SerializeField] private TMP_Text gridLabelPrefab;

        [Header("Placement Popup - Root")]
        [Tooltip("The full popup parent object that gets shown/hidden while placing a ship.")]
        [SerializeField] private GameObject placementPopup;
        [Tooltip("Optional. Assign the RectTransform that should follow the pointer while dragging. Usually this is the PlacementPopup root RectTransform.")]
        [SerializeField] private RectTransform popupFollowTarget;

        [Header("Placement Popup - Split Visuals")]
        [Tooltip("Ship-specific backing image. This changes per ship and should contain only the ship art/name/length backing, not the button box.")]
        [SerializeField] private Image popupBackingImage;
        [Tooltip("Fixed box image containing the ROTATE and CONFIRM POSITION visuals. This does not change per ship.")]
        [SerializeField] private Image popupButtonBoxImage;

        [Header("Placement Popup - Fixed Buttons")]
        [Tooltip("Invisible button positioned over the ROTATE visual area in the fixed button box.")]
        [SerializeField] private Button popupRotateButton;
        [Tooltip("Invisible button positioned over the CONFIRM POSITION visual area in the fixed button box.")]
        [SerializeField] private Button popupConfirmButton;

        [Header("Start Button")]
        [Tooltip("Assign the START button here. If this is left empty, multiplayer placement will request start automatically after all ships are accepted.")]
        [SerializeField] private Button startButton;

        [Tooltip("Optional parent/root for the START visual. If empty, the start button GameObject is shown/hidden directly.")]
        [SerializeField] private GameObject startButtonRoot;

        [Tooltip("Hide START until every ship has been confirmed and there is no active pending placement.")]
        [SerializeField] private bool hideStartButtonUntilReady = true;

        [SerializeField] private GameObject placementPanel;

        [Header("Colors")]
        [SerializeField] private Color emptyCellColor = Color.black;
        [SerializeField] private Color placedCellColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color previewValidColor = new Color(0.36f, 0.78f, 0.50f, 1f);
        [SerializeField] private Color previewInvalidColor = new Color(0.85f, 0.12f, 0.12f, 1f);

        [Header("Placement Radius Cue")]
        [Tooltip("Shows small dots on the one-tile no-placement radius around confirmed ships.")]
        [SerializeField] private bool showPlacementRadiusDots = true;

        [Tooltip("Color for the normal one-tile no-placement radius dots.")]
        [SerializeField] private Color placementRadiusDotColor = new Color(1f, 1f, 1f, 0.85f);

        [Tooltip("Color used when the pending ship overlaps another ship's no-placement radius.")]
        [SerializeField] private Color placementRadiusInvalidColor = new Color(1f, 0.08f, 0.08f, 1f);

        [Tooltip("Size of each no-placement radius dot inside a grid tile.")]
        [SerializeField] private Vector2 placementRadiusDotSize = new Vector2(12f, 12f);

        [Header("Flow")]
        [SerializeField] private bool autoRequestStartWhenAllShipsPlaced = false;
        [SerializeField] private bool hidePlacementPanelOnBattleStart = true;

        // Local placement state is kept here until the server confirms it.
        private readonly Dictionary<string, PlacedShip> confirmedShips = new Dictionary<string, PlacedShip>();
        private readonly Dictionary<string, int> shipIndexByType = new Dictionary<string, int>();
        private readonly HashSet<string> serverAcceptedShips = new HashSet<string>();

        // Generated grid widgets are cached by coordinate for repainting and input control.
        private Button[,] gridButtons = new Button[10, 10];
        private Image[,] gridImages = new Image[10, 10];
        private Image[,] radiusDotImages = new Image[10, 10];

        // Pending placement tracks the ship currently being previewed before confirmation.
        private PlacedShip pendingShip;
        private bool pendingValid;
        private string selectedShip;
        private Orientation currentOrientation = Orientation.Horizontal;
        private Coroutine pulseRoutine;
        private int localPlayerNumber;
        private bool isSubmittingPlacements;
        private Coroutine submitPlacementsRoutine;

        /// <summary>
        /// Caches setup data, binds buttons, builds the grid, and prepares the initial placement state.
        /// </summary>
        private void Start()
        {
            CacheShipIndexes();
            SetupShipButtons();
            SetupPopup();
            SetupStartButton();
            BuildGrid();
            BuildGridLabels();
            HidePopup();

            if (ships != null && ships.Length > 0)
                selectedShip = ships[0].shipType;
        }

        /// <summary>
        /// Subscribes to multiplayer placement and battle-start events.
        /// </summary>
        private void OnEnable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned += OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted += OnShipPlacementAccepted;
            NetworkBattleshipEvents.ShipPlacementRejected += OnShipPlacementRejected;
            NetworkBattleshipEvents.BattleStarted += OnBattleStarted;
        }

        /// <summary>
        /// Unsubscribes from multiplayer placement and battle-start events.
        /// </summary>
        private void OnDisable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned -= OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted -= OnShipPlacementAccepted;
            NetworkBattleshipEvents.ShipPlacementRejected -= OnShipPlacementRejected;
            NetworkBattleshipEvents.BattleStarted -= OnBattleStarted;
        }

        /// <summary>
        /// Builds a lookup table from ship type to inspector array index.
        /// </summary>
        private void CacheShipIndexes()
        {
            shipIndexByType.Clear();
            if (ships == null) return;

            for (int i = 0; i < ships.Length; i++)
            {
                if (ships[i] == null || string.IsNullOrWhiteSpace(ships[i].shipType)) continue;
                shipIndexByType[ships[i].shipType] = i;
            }
        }

        /// <summary>
        /// Connects ship selection buttons and drag handlers for each configured ship option.
        /// </summary>
        private void SetupShipButtons()
        {
            if (ships == null) return;

            for (int i = 0; i < ships.Length; i++)
            {
                int index = i;
                Button button = ships[i].button;
                if (button == null) continue;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => BeginPendingFromList(index, true));

                EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
                trigger.triggers.Clear();

                AddTrigger(trigger, EventTriggerType.BeginDrag, data => BeginPendingFromList(index, false));
                AddTrigger(trigger, EventTriggerType.Drag, data => DragPending((PointerEventData)data));
                AddTrigger(trigger, EventTriggerType.EndDrag, data => EndDragPending((PointerEventData)data));
            }
        }

        /// <summary>
        /// Connects popup rotate/confirm buttons and drag handlers.
        /// </summary>
        private void SetupPopup()
        {
            if (popupRotateButton != null)
            {
                popupRotateButton.onClick.RemoveAllListeners();
                popupRotateButton.onClick.AddListener(RotatePendingShip);
            }

            if (popupConfirmButton != null)
            {
                popupConfirmButton.onClick.RemoveAllListeners();
                popupConfirmButton.onClick.AddListener(ConfirmPendingPlacement);
            }

            if (placementPopup != null)
            {
                EventTrigger popupTrigger = placementPopup.GetComponent<EventTrigger>();
                if (popupTrigger == null) popupTrigger = placementPopup.AddComponent<EventTrigger>();
                popupTrigger.triggers.Clear();
                AddTrigger(popupTrigger, EventTriggerType.BeginDrag, data => BeginPopupDrag((PointerEventData)data));
                AddTrigger(popupTrigger, EventTriggerType.Drag, data => DragPending((PointerEventData)data));
                AddTrigger(popupTrigger, EventTriggerType.EndDrag, data => EndDragPending((PointerEventData)data));
            }
        }

        /// <summary>
        /// Connects the start button and applies its initial visibility state.
        /// </summary>
        private void SetupStartButton()
        {
            if (startButton == null) return;
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(TryStartGame);
            UpdateStartButtonVisibility();
        }

        /// <summary>
        /// Adds an EventTrigger entry that forwards UI pointer events to a callback.
        /// </summary>
        private void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
        }

        /// <summary>
        /// Creates the 10 by 10 placement grid and associated cell images.
        /// </summary>
        private void BuildGrid()
        {
            if (gridRoot == null) return;
            ClearChildren(gridRoot);

            GridLayoutGroup layout = gridRoot.GetComponent<GridLayoutGroup>();
            if (layout == null) layout = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 10;
            layout.cellSize = cellSize;
            layout.spacing = cellSpacing;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    int cx = x;
                    int cy = y;
                    Button cell = CreateGridCell(x, y);
                    cell.onClick.AddListener(() => HandleGridCellClicked(cx, cy));

                    EventTrigger trigger = cell.gameObject.GetComponent<EventTrigger>();
                    if (trigger == null) trigger = cell.gameObject.AddComponent<EventTrigger>();
                    trigger.triggers.Clear();
                    AddTrigger(trigger, EventTriggerType.BeginDrag, data => BeginGridCellDrag(cx, cy, (PointerEventData)data));
                    AddTrigger(trigger, EventTriggerType.Drag, data => DragPending((PointerEventData)data));
                    AddTrigger(trigger, EventTriggerType.EndDrag, data => EndDragPending((PointerEventData)data));

                    gridButtons[x, y] = cell;
                    gridImages[x, y] = cell.GetComponent<Image>();
                    radiusDotImages[x, y] = CreateRadiusDot(cell.transform);
                }
            }
        }

        /// <summary>
        /// Creates one grid button and wires click/drag events for the given coordinate.
        /// </summary>
        private Button CreateGridCell(int x, int y)
        {
            Button cell;
            if (gridCellPrefab != null)
            {
                cell = Instantiate(gridCellPrefab, gridRoot);
                cell.name = $"Cell_{x}_{y}";
            }
            else
            {
                GameObject go = new GameObject($"Cell_{x}_{y}");
                go.transform.SetParent(gridRoot, false);
                Image image = go.AddComponent<Image>();
                image.color = emptyCellColor;
                Outline outline = go.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(2f, -2f);
                cell = go.AddComponent<Button>();
            }

            Image cellImage = cell.GetComponent<Image>();
            cellImage.color = emptyCellColor;
            cellImage.raycastTarget = true;
            return cell;
        }

        /// <summary>
        /// Creates the small visual dot used to show no-placement radius cues.
        /// </summary>
        private Image CreateRadiusDot(Transform parent)
        {
            GameObject dot = new GameObject("PlacementRadiusDot");
            dot.transform.SetParent(parent, false);

            RectTransform rectTransform = dot.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = placementRadiusDotSize;

            Image image = dot.AddComponent<Image>();
            image.color = placementRadiusDotColor;
            image.raycastTarget = false;
            image.enabled = false;

            return image;
        }

        /// <summary>
        /// Builds the row and column labels around the placement grid.
        /// </summary>
        private void BuildGridLabels()
        {
            BuildLabelRow(columnLabelRoot, i => (i + 1).ToString(), 10);
            BuildLabelRow(rowLabelRoot, i => ((char)('A' + i)).ToString(), 1);
        }

        /// <summary>
        /// Creates one row or column of generated coordinate labels.
        /// </summary>
        private void BuildLabelRow(RectTransform root, System.Func<int, string> labelFactory, int constraintCount)
        {
            if (root == null || gridLabelPrefab == null) return;
            ClearChildren(root);

            GridLayoutGroup layout = root.GetComponent<GridLayoutGroup>();
            if (layout == null) layout = root.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = cellSize;
            layout.spacing = cellSpacing;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = constraintCount;

            for (int i = 0; i < 10; i++)
            {
                TMP_Text label = Instantiate(gridLabelPrefab, root);
                label.text = labelFactory(i);
                label.raycastTarget = false;
            }
        }

        /// <summary>
        /// Removes generated child objects before rebuilding labels or grid elements.
        /// </summary>
        private void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        /// <summary>
        /// Starts placing the selected ship from the ship list.
        /// </summary>
        private void BeginPendingFromList(int index, bool clicked)
        {
            if (ships == null || index < 0 || index >= ships.Length) return;
            ShipUIOption option = ships[index];
            if (option == null || string.IsNullOrWhiteSpace(option.shipType)) return;

            selectedShip = option.shipType;
            ApplySelectedShipImages(option);
            currentOrientation = Orientation.Horizontal;

            if (confirmedShips.TryGetValue(selectedShip, out PlacedShip alreadyPlaced))
            {
                currentOrientation = alreadyPlaced.Orientation;
                SetPendingShip(selectedShip, alreadyPlaced.X, alreadyPlaced.Y, alreadyPlaced.Orientation);
            }
            else if (clicked)
            {
                Vector2Int origin = GetCenteredOrigin(selectedShip, currentOrientation);
                SetPendingShip(selectedShip, origin.x, origin.y, currentOrientation);
            }
            else
            {
                SetPendingShip(selectedShip, 0, 0, currentOrientation);
            }

            ShowPopupForShip(selectedShip);
            UpdateShipButtonVisuals();
            UpdateStartButtonVisibility();
        }

        /// <summary>
        /// Allows the visible placement popup to be dragged to a new grid cell.
        /// </summary>
        private void BeginPopupDrag(PointerEventData eventData)
        {
            if (pendingShip == null) return;
            DragPending(eventData);
        }

        /// <summary>
        /// Starts dragging an already confirmed ship from its current grid position.
        /// </summary>
        private void BeginGridCellDrag(int x, int y, PointerEventData eventData)
        {
            PlacedShip placedShip = GetConfirmedShipAt(x, y);

            if (placedShip != null)
            {
                selectedShip = placedShip.Type;
                currentOrientation = placedShip.Orientation;

                SetPendingShip(placedShip.Type, placedShip.X, placedShip.Y, placedShip.Orientation);

                if (shipIndexByType.TryGetValue(placedShip.Type, out int index))
                    ApplySelectedShipImages(ships[index]);

                ShowPopupForShip(placedShip.Type);
            }
            else if (pendingShip == null)
            {
                return;
            }

            DragPending(eventData);
        }

        /// <summary>
        /// Moves the pending ship preview to the grid cell currently under the pointer.
        /// </summary>
        private void DragPending(PointerEventData eventData)
        {
            if (pendingShip == null || gridRoot == null) return;
            if (!TryGetGridCellFromPointer(eventData, out int x, out int y)) return;
            SetPendingShip(selectedShip, x, y, currentOrientation);
            MovePopupToPointer(eventData);
        }

        /// <summary>
        /// Finishes a drag operation while leaving the pending placement ready for confirmation.
        /// </summary>
        private void EndDragPending(PointerEventData eventData)
        {
            if (pendingShip == null || gridRoot == null) return;
            if (TryGetGridCellFromPointer(eventData, out int x, out int y))
                SetPendingShip(selectedShip, x, y, currentOrientation);
            ShowPopupForShip(selectedShip);
        }

        /// <summary>
        /// Converts the current pointer position into a grid coordinate when possible.
        /// </summary>
        private bool TryGetGridCellFromPointer(PointerEventData eventData, out int x, out int y)
        {
            x = -1;
            y = -1;

            Camera eventCamera = eventData.pressEventCamera;
            Canvas parentCanvas = gridRoot.GetComponentInParent<Canvas>();
            if (eventCamera == null && parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = Camera.main;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRoot, eventData.position, eventCamera, out Vector2 local))
                return false;

            Rect rect = gridRoot.rect;
            float left = -gridRoot.pivot.x * rect.width;
            float top = (1f - gridRoot.pivot.y) * rect.height;
            float px = local.x - left;
            float py = top - local.y;

            float strideX = cellSize.x + cellSpacing.x;
            float strideY = cellSize.y + cellSpacing.y;

            // Mobile-friendly nearest-cell targeting. This intentionally treats the visual
            // spacing between grid tiles as part of the nearest tile, so taps/drags near
            // tile edges do not get rejected.
            x = Mathf.RoundToInt((px - cellSize.x * 0.5f) / strideX);
            y = Mathf.RoundToInt((py - cellSize.y * 0.5f) / strideY);

            return IsInsideGrid(x, y);
        }

        /// <summary>
        /// Handles tapping a grid cell to create, move, or edit a pending placement.
        /// </summary>
        private void HandleGridCellClicked(int x, int y)
        {
            PlacedShip placedShip = GetConfirmedShipAt(x, y);
            if (placedShip != null)
            {
                selectedShip = placedShip.Type;
                currentOrientation = placedShip.Orientation;
                SetPendingShip(placedShip.Type, placedShip.X, placedShip.Y, placedShip.Orientation);
                if (shipIndexByType.TryGetValue(placedShip.Type, out int index))
                    ApplySelectedShipImages(ships[index]);
                ShowPopupForShip(placedShip.Type);
                return;
            }

            if (pendingShip != null)
            {
                SetPendingShip(selectedShip, x, y, currentOrientation);
                ShowPopupForShip(selectedShip);
            }
        }

        /// <summary>
        /// Returns the confirmed ship occupying a coordinate, if one exists.
        /// </summary>
        private PlacedShip GetConfirmedShipAt(int x, int y)
        {
            foreach (PlacedShip ship in confirmedShips.Values)
            {
                foreach (Vector2Int cell in GetOccupiedCells(ship))
                    if (cell.x == x && cell.y == y) return ship;
            }
            return null;
        }

        /// <summary>
        /// Stores the current pending placement and updates its preview validity.
        /// </summary>
        private void SetPendingShip(string type, int x, int y, Orientation orientation)
        {
            pendingShip = new PlacedShip(type, x, y, orientation);
            pendingValid = CanPreviewShip(pendingShip);
            RepaintGrid();
            StartPulse();
            UpdateStartButtonVisibility();
        }

        /// <summary>
        /// Checks whether a pending ship fits on the board without overlapping confirmed ships or exclusion radius.
        /// </summary>
        private bool CanPreviewShip(PlacedShip ship)
        {
            foreach (Vector2Int cell in GetOccupiedCells(ship))
            {
                if (!IsInsideGrid(cell.x, cell.y))
                    return false;

                foreach (KeyValuePair<string, PlacedShip> kvp in confirmedShips)
                {
                    // When readjusting an already confirmed ship, ignore its previous
                    // placement and radius. The new placement will replace it.
                    if (kvp.Key == ship.Type)
                        continue;

                    if (IsInsideShipRadius(cell, kvp.Value, true))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Accepts a valid pending placement locally and prepares it for server submission.
        /// </summary>
        private void ConfirmPendingPlacement()
        {
            if (pendingShip == null || isSubmittingPlacements) return;

            if (!pendingValid)
            {
                StartPulse();
                return;
            }

            // Keep multiplayer placement editable locally until the player presses START.
            // The old version sent each confirmation to the multiplayer session immediately;
            // that made the session reject later adjustments with "ship has already been placed."
            confirmedShips[pendingShip.Type] = new PlacedShip(
                pendingShip.Type,
                pendingShip.X,
                pendingShip.Y,
                pendingShip.Orientation
            );

            pendingShip = null;
            HidePopup();
            StopPulse();
            RepaintGrid();
            UpdateShipButtonVisuals();
            SelectNextUnplacedShip();
            UpdateStartButtonVisibility();

            if (autoRequestStartWhenAllShipsPlaced && confirmedShips.Count == ShipOptionCount())
                TryStartGame();
        }

        /// <summary>
        /// Starts the placement submission flow when all required ships are locally placed.
        /// </summary>
        private void TryStartGame()
        {
            if (isSubmittingPlacements)
                return;

            if (pendingShip != null)
            {
                if (!pendingValid)
                {
                    StartPulse();
                    UpdateStartButtonVisibility();
                    return;
                }

                ConfirmPendingPlacement();
            }

            if (confirmedShips.Count != ShipOptionCount())
            {
                Debug.Log("Place and confirm all ships before starting the game.");
                UpdateStartButtonVisibility();
                return;
            }

            if (submitPlacementsRoutine != null)
                StopCoroutine(submitPlacementsRoutine);

            UpdateStartButtonVisibility();
            submitPlacementsRoutine = StartCoroutine(SubmitPlacementsThenStartGame());
        }

        /// <summary>
        /// Submits each locally confirmed ship to the server before requesting the battle start.
        /// </summary>
        private IEnumerator SubmitPlacementsThenStartGame()
        {
            if (session == null)
            {
                Debug.LogWarning("[MultiplayerPlacementUI] Session is missing.");
                yield break;
            }

            isSubmittingPlacements = true;
            UpdateStartButtonVisibility();
            SetPlacementInteractable(false);
            serverAcceptedShips.Clear();

            foreach (ShipUIOption option in ships)
            {
                if (option == null || string.IsNullOrWhiteSpace(option.shipType))
                    continue;

                if (!confirmedShips.TryGetValue(option.shipType, out PlacedShip ship))
                {
                    Debug.LogWarning($"[MultiplayerPlacementUI] Missing local placement for {option.shipType}.");
                    isSubmittingPlacements = false;
                    SetPlacementInteractable(true);
                    yield break;
                }

                selectedShip = ship.Type;
                session.PlaceShip(ship.Type, ship.X, ship.Y, ship.Orientation);

                float timeoutAt = Time.realtimeSinceStartup + 5f;
                while (!serverAcceptedShips.Contains(ship.Type) && Time.realtimeSinceStartup < timeoutAt)
                    yield return null;

                if (!serverAcceptedShips.Contains(ship.Type))
                {
                    Debug.LogWarning($"[MultiplayerPlacementUI] Timed out waiting for server to accept {ship.Type}.");
                    isSubmittingPlacements = false;
                    SetPlacementInteractable(true);
                    yield break;
                }
            }

            RequestStartGame();
        }

        /// <summary>
        /// Enables or disables placement buttons while submissions are in progress.
        /// </summary>
        private void SetPlacementInteractable(bool interactable)
        {
            if (ships != null)
            {
                foreach (ShipUIOption option in ships)
                    if (option != null && option.button != null)
                        option.button.interactable = interactable;
            }

            if (popupRotateButton != null)
                popupRotateButton.interactable = interactable;

            if (popupConfirmButton != null)
                popupConfirmButton.interactable = interactable;

            if (startButton != null)
                startButton.interactable = interactable;
        }

        /// <summary>
        /// Asks the multiplayer session to start the battle after placement submission succeeds.
        /// </summary>
        private void RequestStartGame()
        {
            if (session == null)
            {
                Debug.LogWarning("[MultiplayerPlacementUI] Session is missing.");
                return;
            }

            session.StartGame();
        }

        /// <summary>
        /// Stores the local multiplayer player number.
        /// </summary>
        private void OnLocalPlayerAssigned(int playerNumber)
        {
            localPlayerNumber = playerNumber;
            Debug.Log($"[MultiplayerPlacementUI] You are Player {playerNumber}.");
        }

        /// <summary>
        /// Marks a ship as accepted by the server and advances the placement flow if needed.
        /// </summary>
        private void OnShipPlacementAccepted(int playerNumber, string shipType, int startX, int startY, int orientationValue)
        {
            if (playerNumber != localPlayerNumber)
                return;

            Orientation orientation = (Orientation)orientationValue;
            serverAcceptedShips.Add(shipType);
            confirmedShips[shipType] = new PlacedShip(shipType, startX, startY, orientation);

            Debug.Log($"[MultiplayerPlacementUI] Placed {shipType} at ({startX},{startY}) {orientation}.");

            if (pendingShip != null && pendingShip.Type == shipType)
                pendingShip = null;

            HidePopup();
            StopPulse();
            RepaintGrid();
            UpdateShipButtonVisuals();

            if (!isSubmittingPlacements)
                SelectNextUnplacedShip();

            if (!isSubmittingPlacements && autoRequestStartWhenAllShipsPlaced && confirmedShips.Count == ShipOptionCount())
                TryStartGame();
        }

        /// <summary>
        /// Re-enables placement after the server rejects a submitted ship.
        /// </summary>
        private void OnShipPlacementRejected(int playerNumber, GameErrorCode errorCode)
        {
            if (playerNumber != localPlayerNumber)
                return;

            Debug.LogWarning($"[MultiplayerPlacementUI] Cannot place {selectedShip}: {errorCode}");
            isSubmittingPlacements = false;
            SetPlacementInteractable(true);
            StartPulse();
        }

        /// <summary>
        /// Hides placement UI once the multiplayer battle begins.
        /// </summary>
        private void OnBattleStarted(int startingPlayerNumber)
        {
            Debug.Log($"[MultiplayerPlacementUI] Battle started. Player {startingPlayerNumber} goes first.");

            isSubmittingPlacements = false;
            if (submitPlacementsRoutine != null)
            {
                StopCoroutine(submitPlacementsRoutine);
                submitPlacementsRoutine = null;
            }

            if (!hidePlacementPanelOnBattleStart)
                return;

            if (placementPanel != null)
                placementPanel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Automatically selects the next ship that has not yet been confirmed.
        /// </summary>
        private void SelectNextUnplacedShip()
        {
            if (ships == null) return;

            foreach (ShipUIOption option in ships)
            {
                if (option == null || string.IsNullOrWhiteSpace(option.shipType)) continue;
                if (!confirmedShips.ContainsKey(option.shipType))
                {
                    selectedShip = option.shipType;
                    return;
                }
            }
        }

        /// <summary>
        /// Returns the number of configured ship options while safely handling null arrays.
        /// </summary>
        private int ShipOptionCount()
        {
            int count = 0;
            if (ships == null) return count;

            foreach (ShipUIOption option in ships)
            {
                if (option != null && !string.IsNullOrWhiteSpace(option.shipType))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Repaints confirmed ships, pending placement preview, and radius cues.
        /// </summary>
        private void RepaintGrid()
        {
            ClearGridVisuals();

            foreach (PlacedShip ship in confirmedShips.Values)
            {
                if (pendingShip != null && ship.Type == pendingShip.Type)
                    continue;

                PaintShipCells(ship, placedCellColor);
                PaintShipRadiusDots(ship, placementRadiusDotColor);
            }

            if (pendingShip != null)
            {
                PaintShipCells(pendingShip, pendingValid ? previewValidColor : previewInvalidColor);

                if (!pendingValid)
                    PaintPendingRadiusViolations(pendingShip, placementRadiusInvalidColor);
            }
        }

        /// <summary>
        /// Resets all grid cells and radius dots to their default visual state.
        /// </summary>
        private void ClearGridVisuals()
        {
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    if (gridImages[x, y] != null)
                        gridImages[x, y].color = emptyCellColor;

                    if (radiusDotImages[x, y] != null)
                        radiusDotImages[x, y].enabled = false;
                }
            }
        }

        /// <summary>
        /// Paints radius dots around a confirmed ship.
        /// </summary>
        private void PaintShipRadiusDots(PlacedShip ship, Color color)
        {
            if (!showPlacementRadiusDots)
                return;

            foreach (Vector2Int cell in GetShipRadiusCells(ship, false))
                PaintRadiusDot(cell.x, cell.y, color);
        }

        /// <summary>
        /// Highlights radius dots that conflict with the pending ship placement.
        /// </summary>
        private void PaintPendingRadiusViolations(PlacedShip ship, Color color)
        {
            if (!showPlacementRadiusDots)
                return;

            foreach (Vector2Int cell in GetOccupiedCells(ship))
            {
                if (!IsInsideGrid(cell.x, cell.y))
                    continue;

                foreach (KeyValuePair<string, PlacedShip> kvp in confirmedShips)
                {
                    if (kvp.Key == ship.Type)
                        continue;

                    if (IsInsideShipRadius(cell, kvp.Value, true))
                        PaintRadiusDot(cell.x, cell.y, color);
                }
            }
        }

        /// <summary>
        /// Shows a single radius dot on the requested grid cell.
        /// </summary>
        private void PaintRadiusDot(int x, int y, Color color)
        {
            if (!IsInsideGrid(x, y))
                return;

            Image dot = radiusDotImages[x, y];
            if (dot == null)
                return;

            dot.color = color;
            dot.enabled = true;
            dot.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Checks whether a coordinate lies within a ship's surrounding no-placement radius.
        /// </summary>
        private bool IsInsideShipRadius(Vector2Int cell, PlacedShip ship, bool includeShipCells)
        {
            foreach (Vector2Int radiusCell in GetShipRadiusCells(ship, includeShipCells))
            {
                if (radiusCell.x == cell.x && radiusCell.y == cell.y)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Enumerates all cells in the one-tile radius around a ship.
        /// </summary>
        private IEnumerable<Vector2Int> GetShipRadiusCells(PlacedShip ship, bool includeShipCells)
        {
            HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>(GetOccupiedCells(ship));
            HashSet<Vector2Int> radiusCells = new HashSet<Vector2Int>();

            foreach (Vector2Int occupied in occupiedCells)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        Vector2Int cell = new Vector2Int(occupied.x + offsetX, occupied.y + offsetY);

                        if (!IsInsideGrid(cell.x, cell.y))
                            continue;

                        if (!includeShipCells && occupiedCells.Contains(cell))
                            continue;

                        radiusCells.Add(cell);
                    }
                }
            }

            foreach (Vector2Int cell in radiusCells)
                yield return cell;
        }

        /// <summary>
        /// Colours every cell occupied by a ship placement.
        /// </summary>
        private void PaintShipCells(PlacedShip ship, Color color)
        {
            foreach (Vector2Int cell in GetOccupiedCells(ship))
            {
                if (!IsInsideGrid(cell.x, cell.y)) continue;
                if (gridImages[cell.x, cell.y] != null) gridImages[cell.x, cell.y].color = color;
            }
        }

        /// <summary>
        /// Enumerates the exact board cells occupied by a ship.
        /// </summary>
        private IEnumerable<Vector2Int> GetOccupiedCells(PlacedShip ship)
        {
            int size = Ship.GetSize(ship.Type);
            Vector2Int offset = ship.Orientation == Orientation.Horizontal
                ? new Vector2Int(1, 0)
                : new Vector2Int(0, 1);

            for (int i = 0; i < size; i++)
                yield return new Vector2Int(ship.X + offset.x * i, ship.Y + offset.y * i);
        }

        /// <summary>
        /// Rotates the pending ship and repaints the placement preview.
        /// </summary>
        private void RotatePendingShip()
        {
            currentOrientation = currentOrientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
            if (pendingShip != null)
                SetPendingShip(pendingShip.Type, pendingShip.X, pendingShip.Y, currentOrientation);
        }

        /// <summary>
        /// Chooses a centered starting coordinate for a newly selected ship.
        /// </summary>
        private Vector2Int GetCenteredOrigin(string type, Orientation orientation)
        {
            int size = Ship.GetSize(type);
            int x = orientation == Orientation.Horizontal ? Mathf.FloorToInt((10 - size) * 0.5f) : 4;
            int y = orientation == Orientation.Vertical ? Mathf.FloorToInt((10 - size) * 0.5f) : 4;
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Displays the placement popup for the selected ship type.
        /// </summary>
        private void ShowPopupForShip(string type)
        {
            ShipUIOption option = GetShipOption(type);
            ApplyPopupVisuals(option);
            if (placementPopup != null) placementPopup.SetActive(true);
        }

        /// <summary>
        /// Finds the inspector configuration for a given ship type.
        /// </summary>
        private ShipUIOption GetShipOption(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return null;
            if (!shipIndexByType.TryGetValue(type, out int index)) return null;
            if (ships == null || index < 0 || index >= ships.Length) return null;
            return ships[index];
        }

        /// <summary>
        /// Applies ship-specific popup sprites while preserving fallback artwork.
        /// </summary>
        private void ApplyPopupVisuals(ShipUIOption option)
        {
            if (option == null) return;

            if (popupBackingImage != null)
            {
                popupBackingImage.sprite = option.popupBackingSprite;
                popupBackingImage.enabled = option.popupBackingSprite != null;
                popupBackingImage.raycastTarget = false;
                popupBackingImage.preserveAspect = true;
            }

            if (popupButtonBoxImage != null)
            {
                if (option.popupButtonBoxSprite != null)
                    popupButtonBoxImage.sprite = option.popupButtonBoxSprite;

                popupButtonBoxImage.enabled = popupButtonBoxImage.sprite != null;
                popupButtonBoxImage.raycastTarget = false;
                popupButtonBoxImage.preserveAspect = true;
            }
        }

        /// <summary>
        /// Hides the placement popup and stops its pulse effect.
        /// </summary>
        private void HidePopup()
        {
            if (placementPopup != null) placementPopup.SetActive(false);
        }

        /// <summary>
        /// Positions the popup at the current pointer location.
        /// </summary>
        private void MovePopupToPointer(PointerEventData eventData)
        {
            if (popupFollowTarget == null) return;
            popupFollowTarget.position = eventData.position;
        }

        /// <summary>
        /// Starts the pulsing preview animation for pending ship cells.
        /// </summary>
        private void StartPulse()
        {
            StopPulse();
            if (pendingShip != null) pulseRoutine = StartCoroutine(PulsePendingCells());
        }

        /// <summary>
        /// Stops the pulsing preview animation.
        /// </summary>
        private void StopPulse()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }
        }

        /// <summary>
        /// Animates the pending placement cells by alternating between normal and darker preview colours.
        /// </summary>
        private IEnumerator PulsePendingCells()
        {
            while (pendingShip != null)
            {
                RepaintConfirmedOnly();
                Color baseColor = pendingValid ? previewValidColor : previewInvalidColor;
                float alpha = Mathf.Lerp(0.35f, 1f, Mathf.PingPong(Time.time * 2.5f, 1f));
                Color pulseColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                PaintShipCells(pendingShip, pulseColor);

                if (!pendingValid)
                    PaintPendingRadiusViolations(pendingShip, pulseColor);

                yield return null;
            }
        }

        /// <summary>
        /// Repaints only confirmed ships and their radius indicators.
        /// </summary>
        private void RepaintConfirmedOnly()
        {
            ClearGridVisuals();

            foreach (PlacedShip ship in confirmedShips.Values)
            {
                if (pendingShip != null && ship.Type == pendingShip.Type)
                    continue;

                PaintShipCells(ship, placedCellColor);
                PaintShipRadiusDots(ship, placementRadiusDotColor);
            }
        }

        /// <summary>
        /// Updates the selected-ship display artwork for the active ship option.
        /// </summary>
        private void ApplySelectedShipImages(ShipUIOption option)
        {
            if (option == null) return;
            SetImage(selectedShipNameImage, option.nameSprite);
            SetImage(selectedShipPreviewImage, option.previewSprite);
            SetImage(selectedShipBackingTextImage, option.backingTextSprite);
        }

        /// <summary>
        /// Safely assigns a sprite and visibility state to an image.
        /// </summary>
        private void SetImage(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        /// <summary>
        /// Shows and enables the start button only when placement is ready.
        /// </summary>
        private void UpdateStartButtonVisibility()
        {
            if (startButton == null)
                return;

            bool readyToStart = confirmedShips.Count == ShipOptionCount() && pendingShip == null && !isSubmittingPlacements;
            bool visible = !hideStartButtonUntilReady || readyToStart;

            GameObject target = startButtonRoot != null ? startButtonRoot : startButton.gameObject;
            if (target != null && target.activeSelf != visible)
                target.SetActive(visible);

            // Keep interactable state in sync too. If you assign a parent as StartButtonRoot,
            // the button itself will still be non-clickable until placement is complete.
            startButton.interactable = readyToStart;
        }

        /// <summary>
        /// Disables ship buttons that have already been confirmed.
        /// </summary>
        private void UpdateShipButtonVisuals()
        {
            // Intentionally left blank.
            // Button state visuals are owned by the dedicated button feedback scripts.
        }

        /// <summary>
        /// Checks whether a coordinate is inside the placement board.
        /// </summary>
        private bool IsInsideGrid(int x, int y) => x >= 0 && x < 10 && y >= 0 && y < 10;

        /// <summary>
        /// Stops running coroutines before the placement UI object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            StopPulse();
            if (submitPlacementsRoutine != null)
            {
                StopCoroutine(submitPlacementsRoutine);
                submitPlacementsRoutine = null;
            }
        }
    }
}
