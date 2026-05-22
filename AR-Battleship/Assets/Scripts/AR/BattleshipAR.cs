using UnityEngine;
using Vuforia;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Unity;
using System.Collections.Generic;

/// <summary>
/// Controls the AR (Augmented Reality) game board using Vuforia image tracking.
/// Tracks three image targets: board (enemy grid), aim (targeting), and fire (confirm shot).
/// Handles visual effects (missiles, explosions) and user interaction in AR mode.
/// </summary>
public class BattleshipAR : MonoBehaviour
{
    [Header("Vuforia Targets")]
    public ObserverBehaviour boardObserver;  // Tracks the enemy grid image target
    public ObserverBehaviour aimObserver;    // Tracks the aim/targeting image target
    public ObserverBehaviour confirmObserver; // Tracks the fire/confirm image target

    [Header("Board Settings")]
    public int gridRows = 10;
    public int gridCols = 10;
    public float cellSize = 0.016f; // Size of each cell in Unity units

    [Header("Grid Offset from Tracking Image")]
    public float offsetX = 0f;
    public float offsetZ = 0.15f; // Offsets adjust grid position relative to tracked image

    [Header("Visuals")]
    public Color gridColor = Color.cyan;
    public Color hoverColor = Color.yellow;  // Color when aiming at a cell
    public Color hitColor = Color.red;
    public Color missColor = Color.white;
    public Color labelColor = Color.white;

    // Tracking states for the three image targets
    private bool boardTracked = false;  // Is the enemy grid visible?
    private bool aimTracked = false;    // Is the aim marker visible?
    private bool confirmTracked = false; // Is the fire marker visible?
    private bool confirmJustDetected = false;
    private bool smoothingInitialized = false;

    // Grid state - tracks which cells have been fired and their outcomes
    private GameObject[,] cellObjects;        // Visual cell game objects
    private bool[,] cellFired;                // Has this cell been shot?
    private ShotOutcome?[,] cellOutcomes;     // What was the outcome? (Hit/Miss/Sunk)
    private List<GameObject> spawnedPrefabs = new List<GameObject>();  // All spawned effects (missiles, fires) for cleanup
    
    // UI elements
    private GameObject hoverIndicator; // Visual indicator when aiming
    private GameObject gridBorder;     // Border around the grid
    private GameObject[] columnLabels; // A, B, C... labels
    private GameObject[] rowLabels;    // 1, 2, 3... labels
    
    // Cell selection state
    private Vector2Int currentHoverCell = new Vector2Int(-1, -1);
    private Vector2Int lockedCell = new Vector2Int(-1, -1);  // Cell locked in when aim tag is placed
    private Vector2Int previousHoverCell = new Vector2Int(-1, -1);

    private float boardWidth;
    private float boardHeight;

    // Smooth tracking for aim marker movement
    private Vector3 smoothedLocalPos;
    private float smoothSpeed = 5f;
    
    // Prefabs for visual effects
    public GameObject customCellPrefab;  // Cell tile prefab
    public GameObject rocketPrefab;      // Miss animation prefab
    public GameObject hitRocketPrefab;   // Hit animation prefab
    public Transform targetTile;
    private Color originalTileColor;


    bool IsMirrored
    {
        get
        {
#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

    float CellX(int col)
    {
        int c = IsMirrored ? (gridCols - 1 - col) : col;
        return (c * cellSize) + (cellSize / 2f) - (boardWidth / 2f) + offsetX;
    }

    float CellZ(int row)
    {
        return ((gridRows - 1 - row) * cellSize) + (cellSize / 2f) - (boardHeight / 2f) + offsetZ;
    }

    void Start()
    {
        cellFired = new bool[gridCols, gridRows];
        cellOutcomes = new ShotOutcome?[gridCols, gridRows];
        cellObjects = new GameObject[gridCols, gridRows];

        boardWidth = gridCols * cellSize;
        boardHeight = gridRows * cellSize;

        boardObserver.OnTargetStatusChanged += OnBoardStatusChanged;
        aimObserver.OnTargetStatusChanged += OnAimStatusChanged;
        confirmObserver.OnTargetStatusChanged += OnConfirmStatusChanged;

        CreateGrid();
        CreateCoordinateLabels();
        CreateHoverIndicator();
    }

    // Subscribe to game events - will receive shot notifications even when board isn't visible
    void OnEnable()
    {
        GameManager.OnPlayerShotFired += HandlePlayerShot;
        GameManager.OnEnemyShotFired += HandleEnemyShot;
        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnGameStarted += ResetBoardState;
    }

    // Unsubscribe from events to prevent memory leaks
    void OnDisable()
    {
        GameManager.OnPlayerShotFired -= HandlePlayerShot;
        GameManager.OnEnemyShotFired -= HandleEnemyShot;
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameStarted -= ResetBoardState;
    }

    /// <summary>
    /// Resets the board for a new game.
    /// Destroys all spawned visual effects (missiles, fires, explosions).
    /// Clears tracking data for fired cells.
    /// Cell tiles themselves are NOT destroyed - they persist and are reused.
    /// </summary>
    void ResetBoardState()
    {
        Debug.Log("[AR] Resetting board state for new game");
        
        // Clean up all spawned effects from previous game
        foreach (GameObject prefab in spawnedPrefabs)
        {
            if (prefab != null)
            {
                Destroy(prefab);
            }
        }
        spawnedPrefabs.Clear();
        
        // Clear game state tracking
        for (int x = 0; x < gridCols; x++)
        {
            for (int y = 0; y < gridRows; y++)
            {
                cellFired[x, y] = false;
                cellOutcomes[x, y] = null;
                
                // Cell colors are NOT reset here - they maintain their prefab's default appearance
                // Visual effects (fire, splash) are separate spawned objects, not cell modifications
            }
        }
    }

    /// <summary>
    /// Spawns a missile that flies down and creates a splash effect when missing.
    /// Adds to spawnedPrefabs list for cleanup when game resets.
    /// </summary>
    void SpawnMissRocket(int col, int row) {
        if (rocketPrefab == null)
        {
            Debug.LogWarning("[AR] rocketPrefab is null - skipping spawn (likely in 2D mode)");
            return;
        }

        GameObject chosenTile = cellObjects[col, row];

        GameObject rocket = Instantiate(rocketPrefab);
        spawnedPrefabs.Add(rocket);  // Track for cleanup
        rocket.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        rocket.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

        MissileDropMissAlt missileScript =
            rocket.GetComponentInChildren<MissileDropMissAlt>();

        missileScript.missTargetTileAlt = chosenTile.transform;
    }

    /// <summary>
    /// Spawns a missile that flies down and creates a fire/explosion effect when hitting a ship.
    /// The fire effect shows which ship was hit and which segment.
    /// </summary>
    void SpawnHitRocket(int col, int row, int? hitSegmentIndex, string? shipOrientation, string shipType) {
        if (hitRocketPrefab == null)
        {
            Debug.LogWarning("[AR] hitRocketPrefab is null - skipping spawn (likely in 2D mode)");
            return;
        }

        GameObject chosenTile = cellObjects[col, row];

        GameObject rocket = Instantiate(hitRocketPrefab);
        spawnedPrefabs.Add(rocket);  // Track for cleanup

        rocket.transform.localScale =
            new Vector3(0.005f, 0.005f, 0.005f);

        rocket.transform.rotation =
            Quaternion.Euler(180f, 0f, 0f);

        MoveToTarget rocketScript =
            rocket.GetComponentInChildren<MoveToTarget>();

        rocketScript.chosenTile =
            chosenTile.transform;

        rocketScript.hitSegmentIndex =
            hitSegmentIndex ?? 0;

        rocketScript.shipOrientation =
            shipOrientation ?? "Horizontal";

        rocketScript.shipType =
            shipType;
    }

    /// <summary>
    /// Handles when the player fires a shot at the enemy board.
    /// ALWAYS tracks the shot in cellFired and cellOutcomes, even if board isn't visible.
    /// Only spawns visual effects (missile, fire) if the AR board is currently being tracked.
    /// This allows switching between 2D and AR modes mid-game without losing state.
    /// </summary>
    void HandlePlayerShot(int x, int y, ShotOutcome result, int? hitSegmentIndex, string? shipOrientation, string? shipType)
    {
        // Track state regardless of visibility - this persists when switching between 2D/AR modes
        if (x >= 0 && x < gridCols && y >= 0 && y < gridRows)
        {
            cellFired[x, y] = true;
            cellOutcomes[x, y] = result;
        }

        // Only spawn visual effects if AR board is currently visible
        if (!boardTracked)
        {
            Debug.Log($"[AR] Shot tracked but board not visible - will sync when board detected");
            return;
        }

        hoverIndicator.SetActive(false);
        lockedCell = new Vector2Int(-1, -1);

        // Spawn appropriate visual effect based on outcome
        if (result == ShotOutcome.Miss)
        {
            SpawnMissRocket(x, y);
        }
        else if (result == ShotOutcome.Hit || result == ShotOutcome.Sunk)
        {
            UnityEngine.Debug.Log($"HandlePlayerShot shipType in AR: {shipType}");
            SpawnHitRocket(x, y, hitSegmentIndex, shipOrientation, shipType);
        }
        Debug.Log($"[AR] Shot at ({x},{y}) -> {result}");
        Debug.Log($"[AR] Shot at a {shipOrientation} {shipType} at {hitSegmentIndex}");
    }

    /// <summary>
    /// Handles when the enemy fires a shot at the player's board.
    /// Enemy shots are displayed on the minimap (handled by CombatUI), NOT on the AR board.
    /// AR board only shows player's attacks against enemy.
    /// </summary>
    void HandleEnemyShot(int x, int y, ShotOutcome result, int? hitSegmentIndex, string? shipOrientation, string? shipType)
    {
        Debug.Log($"[AR] Enemy shot at ({x},{y}) -> {result}");
        
        // Enemy shots go to the minimap - CombatUI handles this
        // AR board only shows player's shots, not enemy's shots
        // Do nothing here
    }

    /// <summary>
    /// Syncs the AR board's visual state when it becomes visible.
    /// NOTE: Does NOT re-spawn effects (missiles, fires) - those are one-time animations.
    /// Only ensures cells that were fired are visible.
    /// If you fired shots while in 2D mode, the effects won't replay in AR mode.
    /// </summary>
    void SyncBoardVisuals()
    {
        Debug.Log("[AR] Syncing board visuals with game state");
        // Visual effects (missiles, fires) were spawned when shots occurred
        // We don't re-spawn them - they're one-time animations
        // The cellFired/cellOutcomes data is for game logic, not visual reconstruction
        
        // Just ensure cells that were fired are visible
        for (int x = 0; x < gridCols; x++)
        {
            for (int y = 0; y < gridRows; y++)
            {
                if (cellFired[x, y] && cellObjects[x, y] != null)
                {
                    cellObjects[x, y].SetActive(true);
                }
            }
        }
    }

    void HandleGameOver(int winnerIndex)
    {
        Debug.Log(winnerIndex == 0 ? "[AR] Player wins!" : "[AR] Enemy wins!");
    }

    void CreateGrid()
    {
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridCols; col++)
            {
                GameObject cell = Instantiate(customCellPrefab);
                cell.name = $"Cell_{col}_{row}";
                cell.transform.SetParent(boardObserver.transform);

                cell.transform.localPosition = new Vector3(CellX(col), 0.001f, CellZ(row));
                cell.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                cell.transform.localScale = new Vector3(0.75f, 0.12f, 0.75f);

                Renderer rend = cell.GetComponent<Renderer>();
                originalTileColor = rend.material.color;
                Destroy(cell.GetComponent<Collider>());
                cell.SetActive(false);
                cellObjects[col, row] = cell;
            }
        }

        CreateGridBorder();
    }

    void CreateGridBorder()
    {
        gridBorder = GameObject.CreatePrimitive(PrimitiveType.Quad);
        gridBorder.name = "GridBorder";
        gridBorder.transform.SetParent(boardObserver.transform);
        gridBorder.transform.localPosition = new Vector3(offsetX, 0.0005f, offsetZ);
        gridBorder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        gridBorder.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);

        Renderer rend = gridBorder.GetComponent<Renderer>();
        rend.material = CreateCellMaterial(Color.white, 0.15f);
        Destroy(gridBorder.GetComponent<Collider>());
        gridBorder.SetActive(false);
    }

    void CreateCoordinateLabels()
    {
        float halfW = boardWidth / 2f;
        float halfH = boardHeight / 2f;
        float labelOffset = cellSize * 0.7f;
        float labelYRot = IsMirrored ? 180f : 0f;
        float labelXScale = IsMirrored ? -1f : 1f;

        columnLabels = new GameObject[gridCols];
        for (int col = 0; col < gridCols; col++)
        {
            GameObject label = new GameObject($"ColLabel_{col}");
            label.transform.SetParent(boardObserver.transform);

            float x = CellX(col);
            float z = halfH + offsetZ + labelOffset;
            label.transform.localPosition = new Vector3(x, 0.002f, z);
            label.transform.localRotation = Quaternion.Euler(90f, labelYRot, 0f);
            label.transform.localScale = new Vector3(labelXScale, 1f, 1f);

            TextMesh tm = label.AddComponent<TextMesh>();
            tm.text = ((char)('A' + col)).ToString();
            tm.fontSize = 50;
            tm.characterSize = cellSize * 0.25f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = labelColor;

            label.SetActive(false);
            columnLabels[col] = label;
        }

        rowLabels = new GameObject[gridRows];
        for (int row = 0; row < gridRows; row++)
        {
            GameObject label = new GameObject($"RowLabel_{row}");
            label.transform.SetParent(boardObserver.transform);

            float x = IsMirrored ? (halfW + offsetX + labelOffset) : (-halfW + offsetX - labelOffset);
            float z = CellZ(row);
            label.transform.localPosition = new Vector3(x, 0.002f, z);
            label.transform.localRotation = Quaternion.Euler(90f, labelYRot, 0f);
            label.transform.localScale = new Vector3(labelXScale, 1f, 1f);

            TextMesh tm = label.AddComponent<TextMesh>();
            tm.text = (row + 1).ToString();
            tm.fontSize = 50;
            tm.characterSize = cellSize * 0.25f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = labelColor;

            label.SetActive(false);
            rowLabels[row] = label;
        }
    }

    void CreateHoverIndicator()
    {
        hoverIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hoverIndicator.name = "HoverIndicator";
        hoverIndicator.transform.SetParent(boardObserver.transform);
        hoverIndicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        hoverIndicator.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f);

        Renderer rend = hoverIndicator.GetComponent<Renderer>();
        rend.material = CreateCellMaterial(hoverColor, 0.5f);
        Destroy(hoverIndicator.GetComponent<Collider>());
        hoverIndicator.SetActive(false);
    }

    Material CreateCellMaterial(Color color, float alpha)
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        Color c = color;
        c.a = alpha;
        mat.color = c;
        return mat;
    }

    void SetCellColor(int col, int row, Color color, float alpha)
    {
        Renderer rend = cellObjects[col, row].GetComponent<Renderer>();
        Color c = color;
        c.a = alpha;
        rend.material.color = c;
    }

    void OnBoardStatusChanged(ObserverBehaviour b, TargetStatus status)
    {
        bool wasTracked = boardTracked;
        boardTracked = status.Status == Status.TRACKED
                    || status.Status == Status.EXTENDED_TRACKED;

        for (int row = 0; row < gridRows; row++)
            for (int col = 0; col < gridCols; col++)
                cellObjects[col, row].SetActive(boardTracked);

        gridBorder.SetActive(boardTracked);

        for (int col = 0; col < gridCols; col++)
            columnLabels[col].SetActive(boardTracked);
        for (int row = 0; row < gridRows; row++)
            rowLabels[row].SetActive(boardTracked);

        // Sync visual state when board becomes visible
        if (boardTracked && !wasTracked)
        {
            SyncBoardVisuals();
        }

        if (!boardTracked)
        {
            lockedCell = new Vector2Int(-1, -1);
            confirmJustDetected = false;
        }
    }

    void OnAimStatusChanged(ObserverBehaviour b, TargetStatus status)
    {
        aimTracked = status.Status == Status.TRACKED;
        if (!aimTracked)
        {
            smoothingInitialized = false;
        }
    }

    void OnConfirmStatusChanged(ObserverBehaviour b, TargetStatus status)
    {
        bool wasTracked = confirmTracked;
        confirmTracked = status.Status == Status.TRACKED;
        if (!wasTracked && confirmTracked)
        {
            confirmJustDetected = true;
        }
    }

    Vector2Int GetCellFromObserver(ObserverBehaviour observer)
    {
        Vector3 localPos = boardObserver.transform.InverseTransformPoint(
            observer.transform.position
        );

        if (!smoothingInitialized)
        {
            smoothedLocalPos = localPos;
            smoothingInitialized = true;
        }
        else
        {
            smoothedLocalPos = Vector3.Lerp(smoothedLocalPos, localPos, Time.deltaTime * smoothSpeed);
        }
        localPos = smoothedLocalPos;

        float halfW = boardWidth / 2f;
        float halfH = boardHeight / 2f;

        float gridLocalX = localPos.x - offsetX;
        float gridLocalZ = localPos.z - offsetZ;

        int col;
        if (IsMirrored)
            col = (gridCols - 1) - Mathf.FloorToInt((gridLocalX + halfW) / cellSize);
        else
            col = Mathf.FloorToInt((gridLocalX + halfW) / cellSize);

        int row = (gridRows - 1) - Mathf.FloorToInt((gridLocalZ + halfH) / cellSize);

        bool valid = col >= 0 && col < gridCols
                  && row >= 0 && row < gridRows
                  && Mathf.Abs(localPos.y) < 5.0f;

        if (valid)
            return new Vector2Int(col, row);
        else
            return new Vector2Int(-1, -1);
    }

    void Update()
    {
        if (hoverIndicator == null)
            return;

        if (!boardTracked)
        {
            hoverIndicator.SetActive(false);
            return;
        }

        if (aimTracked)
        {
            Vector2Int cell = GetCellFromObserver(aimObserver);
            if (cell.x >= 0 && !cellFired[cell.x, cell.y])
            {
                // Restore old tile
                if (previousHoverCell.x >= 0 &&
                    previousHoverCell != cell)
                {
                    SetCellColor(
                        previousHoverCell.x,
                        previousHoverCell.y,
                        originalTileColor,
                        originalTileColor.a
                    );
                }

                currentHoverCell = cell;
                lockedCell = cell;

                SetCellColor(cell.x, cell.y, new Color(1f, 0.5f, 0f), 1f);

                previousHoverCell = cell;
            }
            else if (cell.x >= 0 && cellFired[cell.x, cell.y])
            {
                hoverIndicator.SetActive(false);
                lockedCell = new Vector2Int(-1, -1);
            }
            else
            {
                hoverIndicator.SetActive(false);
                currentHoverCell = new Vector2Int(-1, -1);
                lockedCell = new Vector2Int(-1, -1);
            }
        }
        else if (lockedCell.x >= 0)
        {
            Renderer hRend = hoverIndicator.GetComponent<Renderer>();
            Color c = Color.Lerp(hoverColor, hitColor, Mathf.PingPong(Time.time * 2f, 1f));
            c.a = 0.6f;
            hRend.material.color = c;
        }
        else
        {
            if (previousHoverCell.x >= 0)
            {
                SetCellColor(
                    previousHoverCell.x,
                    previousHoverCell.y,
                    gridColor,
                    1f
                );
            }

            previousHoverCell = new Vector2Int(-1, -1);

            currentHoverCell = new Vector2Int(-1, -1);
        }

        if (confirmJustDetected)
        {
            confirmJustDetected = false;
            TryConfirmShot();
        }
    }

    void TryConfirmShot()
    {
        if (lockedCell.x < 0 || lockedCell.y < 0) return;
        if (GameManager.Instance.CurrentPlayerTurn != 0) return;

        ShotOutcome result = GameManager.Instance.FireShot(lockedCell.x, lockedCell.y);
        if (result == ShotOutcome.None)
        {
            Debug.Log($"[AR] Shot at ({lockedCell.x},{lockedCell.y}) not processed.");
        }
    }
}