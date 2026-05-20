using UnityEngine;
using Vuforia;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Unity;

public class BattleshipAR : MonoBehaviour
{
    [Header("Vuforia Targets")]
    public ObserverBehaviour boardObserver;
    public ObserverBehaviour aimObserver;
    public ObserverBehaviour confirmObserver;

    [Header("Board Settings")]
    public int gridRows = 10;
    public int gridCols = 10;
    public float cellSize = 0.016f;

    [Header("Grid Offset from Tracking Image")]
    public float offsetX = 0f;
    public float offsetZ = 0.15f;

    [Header("Visuals")]
    public Color gridColor = Color.cyan;
    public Color hoverColor = Color.yellow;
    public Color hitColor = Color.red;
    public Color missColor = Color.white;
    public Color labelColor = Color.white;

    private bool boardTracked = false;
    private bool aimTracked = false;
    private bool confirmTracked = false;
    private bool confirmJustDetected = false;
    private bool smoothingInitialized = false;

    private GameObject[,] cellObjects;
    private bool[,] cellFired;
    private GameObject hoverIndicator;
    private GameObject gridBorder;
    private GameObject[] columnLabels;
    private GameObject[] rowLabels;
    private Vector2Int currentHoverCell = new Vector2Int(-1, -1);
    private Vector2Int lockedCell = new Vector2Int(-1, -1);
    private Vector2Int previousHoverCell = new Vector2Int(-1, -1);

    private float boardWidth;
    private float boardHeight;

    private Vector3 smoothedLocalPos;
    private float smoothSpeed = 5f;
    //Michael crap
    public GameObject customCellPrefab;
    //Michael again
    public GameObject rocketPrefab;
    //and again
    public GameObject hitRocketPrefab;
    //and again
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

    void OnEnable()
    {
        GameManager.OnPlayerShotFired += HandlePlayerShot;
        GameManager.OnGameOver += HandleGameOver;
    }

    void OnDisable()
    {
        GameManager.OnPlayerShotFired -= HandlePlayerShot;
        GameManager.OnGameOver -= HandleGameOver;
    }

    void SpawnMissRocket(int col, int row) {
        GameObject chosenTile = cellObjects[col, row];

        GameObject rocket = Instantiate(rocketPrefab);
        rocket.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        rocket.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);

        MissileDropMissAlt missileScript =
            rocket.GetComponentInChildren<MissileDropMissAlt>();

        missileScript.missTargetTileAlt = chosenTile.transform;
    }

    void SpawnHitRocket(int col, int row, int? hitSegmentIndex, string? shipOrientation, string shipType) {
        GameObject chosenTile = cellObjects[col, row];

        GameObject rocket = Instantiate(hitRocketPrefab);

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

    void HandlePlayerShot(int x, int y, ShotOutcome result, int? hitSegmentIndex, string? shipOrientation, string? shipType)
    {
        cellFired[x, y] = true;
        hoverIndicator.SetActive(false);
        lockedCell = new Vector2Int(-1, -1);

        if (result == ShotOutcome.Miss)
        {
            //SetCellColor(x, y, missColor, 0.7f); //sams stuff
            SpawnMissRocket(x, y);
            //MissTileMarkerAlt marker = cellObjects[x, y].GetComponent<MissTileMarkerAlt>();
            //if (marker != null)
            //{
            //    marker.TriggerMissVisualAlt();
            //}
        }
        else if (result == ShotOutcome.Hit || result == ShotOutcome.Sunk)
        {
            //SetCellColor(x, y, hitColor, 0.7f);
            UnityEngine.Debug.Log($"HandlePlayerShot shipType in AR: {shipType}");
            SpawnHitRocket(x, y, hitSegmentIndex, shipOrientation, shipType);
        }
        Debug.Log($"[AR] Shot at ({x},{y}) -> {result}");
        Debug.Log($"[AR] Shot at a {shipOrientation} {shipType} at {hitSegmentIndex}");
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
                //GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad); //sams stuff
                GameObject cell = Instantiate(customCellPrefab); //michael stuff
                cell.name = $"Cell_{col}_{row}";
                cell.transform.SetParent(boardObserver.transform);

                cell.transform.localPosition = new Vector3(CellX(col), 0.001f, CellZ(row));
                //cell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); //sams stuff
                //cell.transform.localScale = new Vector3(cellSize * 0.9f, cellSize * 0.9f, 1f); //sams stuff
                cell.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                cell.transform.localScale = new Vector3(0.75f, 0.12f, 0.75f);

                Renderer rend = cell.GetComponent<Renderer>();
                originalTileColor = rend.material.color;
                //rend.material = CreateCellMaterial(gridColor, 0.3f); //sams stuff
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
        if (!boardTracked)
        {
            hoverIndicator.SetActive(false);
            return;
        }

        if (aimTracked)
        {
            Vector2Int cell = GetCellFromObserver(aimObserver);

            //if (cell.x >= 0 && !cellFired[cell.x, cell.y])
            //{
            //    currentHoverCell = cell;
            //    lockedCell = cell;

            //    /*hoverIndicator.transform.localPosition = new Vector3(CellX(cell.x), 0.003f, CellZ(cell.y));
            //    hoverIndicator.SetActive(true);

            //    Renderer hRend = hoverIndicator.GetComponent<Renderer>();
            //    Color c = hoverColor;
            //    c.a = 0.5f;
            //    hRend.material.color = c;*/
            //    SetCellColor(cell.x, cell.y, new Color(1f, 0.5f, 0f), 1f);
            //}
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
                    //gridColor,
                    //1f
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
            //hoverIndicator.SetActive(false);
            //currentHoverCell = new Vector2Int(-1, -1);
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