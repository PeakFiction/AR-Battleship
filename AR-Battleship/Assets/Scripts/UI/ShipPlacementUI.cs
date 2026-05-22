using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ARBattleship.Core.Domain;
using ARBattleship.Unity;

namespace ARBattleship.Unity.UI
{
    public class ShipPlacementUI : MonoBehaviour
    {
        [System.Serializable]
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
        [Tooltip("Assign the START button here.")]
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
        [SerializeField] private Color placedButtonColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [Header("Flow")]
        [SerializeField] private bool resetGameOnStart = true;
        [SerializeField] private bool hidePlacementPanelOnStart = true;

        private readonly Dictionary<string, PlacedShip> confirmedShips = new Dictionary<string, PlacedShip>();
        private readonly Dictionary<string, int> shipIndexByType = new Dictionary<string, int>();

        private Button[,] gridButtons = new Button[10, 10];
        private Image[,] gridImages = new Image[10, 10];
        private Image[,] radiusDotImages = new Image[10, 10];

        private PlacedShip pendingShip;
        private bool pendingValid;
        private string selectedShip;
        private Orientation currentOrientation = Orientation.Horizontal;
        private Coroutine pulseRoutine;

        private void Start()
        {
            if (resetGameOnStart && GameManager.Instance != null)
                GameManager.Instance.ResetGame();

            CacheShipIndexes();
            SetupShipButtons();
            SetupPopup();
            SetupStartButton();
            BuildGrid();
            BuildGridLabels();
            HidePopup();
            UpdateStartButtonVisibility();
        }

        private void CacheShipIndexes()
        {
            shipIndexByType.Clear();
            for (int i = 0; i < ships.Length; i++)
            {
                if (ships[i] == null || string.IsNullOrWhiteSpace(ships[i].shipType)) continue;
                shipIndexByType[ships[i].shipType] = i;
            }
        }

        private void SetupShipButtons()
        {
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

        private void SetupStartButton()
        {
            if (startButton == null) return;
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(TryStartGame);
            UpdateStartButtonVisibility();
        }

        private void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
        }

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

        private void BuildGridLabels()
        {
            BuildLabelRow(columnLabelRoot, i => (i + 1).ToString(), 10);
            BuildLabelRow(rowLabelRoot, i => ((char)('A' + i)).ToString(), 1);
        }

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

        private void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }

        private void BeginPendingFromList(int index, bool clicked)
        {
            if (index < 0 || index >= ships.Length) return;
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

        private void BeginPopupDrag(PointerEventData eventData)
        {
            if (pendingShip == null) return;
            DragPending(eventData);
        }

        private void BeginGridCellDrag(int x, int y, PointerEventData eventData)
        {
            PlacedShip placedShip = GetConfirmedShipAt(x, y);

            if (placedShip != null)
            {
                selectedShip = placedShip.Type;
                currentOrientation = placedShip.Orientation;

                SetPendingShip(
                    placedShip.Type,
                    placedShip.X,
                    placedShip.Y,
                    placedShip.Orientation
                );

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

        private void DragPending(PointerEventData eventData)
        {
            if (pendingShip == null || gridRoot == null) return;
            if (!TryGetGridCellFromPointer(eventData, out int x, out int y)) return;
            SetPendingShip(selectedShip, x, y, currentOrientation);
            MovePopupToPointer(eventData);
        }

        private void EndDragPending(PointerEventData eventData)
        {
            if (pendingShip == null || gridRoot == null) return;
            if (TryGetGridCellFromPointer(eventData, out int x, out int y))
                SetPendingShip(selectedShip, x, y, currentOrientation);
            ShowPopupForShip(selectedShip);
        }

        private bool TryGetGridCellFromPointer(PointerEventData eventData, out int x, out int y)
        {
            x = -1;
            y = -1;

            Camera eventCamera = eventData.pressEventCamera;
            Canvas parentCanvas = gridRoot.GetComponentInParent<Canvas>();
            if (eventCamera == null && parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = Camera.main;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    gridRoot, eventData.position, eventCamera, out Vector2 local))
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

        private PlacedShip GetConfirmedShipAt(int x, int y)
        {
            foreach (PlacedShip ship in confirmedShips.Values)
            {
                foreach (Vector2Int cell in GetOccupiedCells(ship))
                    if (cell.x == x && cell.y == y) return ship;
            }
            return null;
        }

        private void SetPendingShip(string type, int x, int y, Orientation orientation)
        {
            pendingShip = new PlacedShip(type, x, y, orientation);
            pendingValid = CanPreviewShip(pendingShip);
            RepaintGrid();
            StartPulse();
            UpdateStartButtonVisibility();
        }

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

        private void ConfirmPendingPlacement()
        {
            if (pendingShip == null) return;

            if (!pendingValid)
            {
                StartPulse();
                return;
            }

            confirmedShips[pendingShip.Type] = pendingShip;
            pendingShip = null;
            HidePopup();
            StopPulse();
            RepaintGrid();
            UpdateShipButtonVisuals();
            UpdateStartButtonVisibility();
        }

        private void TryStartGame()
        {
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

            if (confirmedShips.Count != ships.Length)
            {
                Debug.Log("Place and confirm all ships before starting the game.");
                UpdateStartButtonVisibility();
                return;
            }

            CommitShipsAndStartGame();
        }

        private void CommitShipsAndStartGame()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("Cannot start game: GameManager.Instance is missing.");
                return;
            }

            GameManager.Instance.ResetGame();

            foreach (ShipUIOption option in ships)
            {
                if (!confirmedShips.TryGetValue(option.shipType, out PlacedShip ship))
                {
                    Debug.LogError($"Missing confirmed placement for {option.shipType}.");
                    return;
                }

                bool placed = GameManager.Instance.PlaceShip(
                    ship.Type,
                    new Coordinate(ship.X, ship.Y),
                    ship.Orientation
                );

                if (!placed)
                {
                    Debug.LogError($"GameManager rejected {ship.Type} at ({ship.X},{ship.Y}) {ship.Orientation}.");
                    GameManager.Instance.ResetGame();
                    return;
                }
            }

            GameManager.Instance.StartGame();

            if (hidePlacementPanelOnStart)
            {
                if (placementPanel != null) placementPanel.SetActive(false);
                else gameObject.SetActive(false);
            }
        }

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

        private void PaintShipRadiusDots(PlacedShip ship, Color color)
        {
            if (!showPlacementRadiusDots)
                return;

            foreach (Vector2Int cell in GetShipRadiusCells(ship, false))
                PaintRadiusDot(cell.x, cell.y, color);
        }

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

        private bool IsInsideShipRadius(Vector2Int cell, PlacedShip ship, bool includeShipCells)
        {
            foreach (Vector2Int radiusCell in GetShipRadiusCells(ship, includeShipCells))
            {
                if (radiusCell.x == cell.x && radiusCell.y == cell.y)
                    return true;
            }

            return false;
        }

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

        private void PaintShipCells(PlacedShip ship, Color color)
        {
            foreach (Vector2Int cell in GetOccupiedCells(ship))
            {
                if (!IsInsideGrid(cell.x, cell.y)) continue;
                if (gridImages[cell.x, cell.y] != null) gridImages[cell.x, cell.y].color = color;
            }
        }

        private IEnumerable<Vector2Int> GetOccupiedCells(PlacedShip ship)
        {
            int size = ARBattleship.Core.Domain.Ship.GetSize(ship.Type);
            Vector2Int offset = ship.Orientation == Orientation.Horizontal
                ? new Vector2Int(1, 0)
                : new Vector2Int(0, 1);

            for (int i = 0; i < size; i++)
                yield return new Vector2Int(ship.X + offset.x * i, ship.Y + offset.y * i);
        }

        private void RotatePendingShip()
        {
            currentOrientation = currentOrientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
            if (pendingShip != null)
                SetPendingShip(pendingShip.Type, pendingShip.X, pendingShip.Y, currentOrientation);
        }

        private Vector2Int GetCenteredOrigin(string type, Orientation orientation)
        {
            int size = ARBattleship.Core.Domain.Ship.GetSize(type);
            int x = orientation == Orientation.Horizontal ? Mathf.FloorToInt((10 - size) * 0.5f) : 4;
            int y = orientation == Orientation.Vertical ? Mathf.FloorToInt((10 - size) * 0.5f) : 4;
            return new Vector2Int(x, y);
        }

        private void ShowPopupForShip(string type)
        {
            ShipUIOption option = GetShipOption(type);
            ApplyPopupVisuals(option);
            if (placementPopup != null) placementPopup.SetActive(true);
        }

        private ShipUIOption GetShipOption(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return null;
            if (!shipIndexByType.TryGetValue(type, out int index)) return null;
            if (ships == null || index < 0 || index >= ships.Length) return null;
            return ships[index];
        }

        private void ApplyPopupVisuals(ShipUIOption option)
        {
            if (option == null) return;

            if (popupBackingImage != null)
            {
                popupBackingImage.sprite = option.popupBackingSprite;
                popupBackingImage.enabled = option.popupBackingSprite != null;
                popupBackingImage.raycastTarget = false;

                // The backing art is visual only. The fixed invisible buttons handle interaction.
                // Use the RectTransform to place/scale this object in Unity, not SetNativeSize().
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

        private void HidePopup()
        {
            if (placementPopup != null) placementPopup.SetActive(false);
        }

        private void MovePopupToPointer(PointerEventData eventData)
        {
            if (popupFollowTarget == null) return;
            popupFollowTarget.position = eventData.position;
        }

        private void StartPulse()
        {
            StopPulse();
            if (pendingShip != null) pulseRoutine = StartCoroutine(PulsePendingCells());
        }

        private void StopPulse()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }
        }

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

        private void ApplySelectedShipImages(ShipUIOption option)
        {
            SetImage(selectedShipNameImage, option.nameSprite);
            SetImage(selectedShipPreviewImage, option.previewSprite);
            SetImage(selectedShipBackingTextImage, option.backingTextSprite);
        }

        private void SetImage(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void UpdateStartButtonVisibility()
        {
            if (startButton == null)
                return;

            bool readyToStart = ships != null && confirmedShips.Count == ships.Length && pendingShip == null;
            bool visible = !hideStartButtonUntilReady || readyToStart;

            GameObject target = startButtonRoot != null ? startButtonRoot : startButton.gameObject;
            if (target != null && target.activeSelf != visible)
                target.SetActive(visible);

            // Keep interactable state in sync too. If you assign a parent as StartButtonRoot,
            // the button itself will still be non-clickable until placement is complete.
            startButton.interactable = readyToStart;
        }

        private void UpdateShipButtonVisuals()
        {
            // Intentionally left blank.
            //
            // ShipPlacementUI used to change each ship button's TMP_Text color after
            // placement. That conflicted with CanvasUIButtonFeedback / button-state
            // scripts, which now own button visuals.
            //
            // Placement state is still tracked by confirmedShips and grid colors.
        }

        private bool IsInsideGrid(int x, int y) => x >= 0 && x < 10 && y >= 0 && y < 10;

        private void OnDestroy()
        {
            StopPulse();
        }
    }
}
