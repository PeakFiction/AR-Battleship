using UnityEngine;
using Vuforia;

namespace ARBattleship.Multiplayer.Battleship
{
    public sealed class MultiplayerARInputAdapter : MonoBehaviour
    {
        [Header("Vuforia Targets")]
        [SerializeField] private ObserverBehaviour boardObserver;
        [SerializeField] private ObserverBehaviour aimObserver;
        [SerializeField] private ObserverBehaviour confirmObserver;

        [Header("Multiplayer")]
        [SerializeField] private MultiplayerBattleshipSession multiplayerSession;

        [Header("Board Settings")]
        [SerializeField] private int gridRows = 10;
        [SerializeField] private int gridCols = 10;
        [SerializeField] private float cellSize = 0.016f;

        [Header("Grid Offset from Tracking Image")]
        [SerializeField] private float offsetX = 0f;
        [SerializeField] private float offsetZ = 0.15f;

        private bool boardTracked;
        private bool aimTracked;
        private bool confirmTracked;
        private bool confirmJustDetected;
        private bool smoothingInitialized;

        private float boardWidth;
        private float boardHeight;

        private Vector3 smoothedLocalPos;
        private readonly float smoothSpeed = 5f;

        private Vector2Int lockedCell = new Vector2Int(-1, -1);

        private void Start()
        {
            boardWidth = gridCols * cellSize;
            boardHeight = gridRows * cellSize;

            boardObserver.OnTargetStatusChanged += OnBoardStatusChanged;
            aimObserver.OnTargetStatusChanged += OnAimStatusChanged;
            confirmObserver.OnTargetStatusChanged += OnConfirmStatusChanged;
        }

        private void OnDestroy()
        {
            if (boardObserver != null)
            {
                boardObserver.OnTargetStatusChanged -= OnBoardStatusChanged;
            }

            if (aimObserver != null)
            {
                aimObserver.OnTargetStatusChanged -= OnAimStatusChanged;
            }

            if (confirmObserver != null)
            {
                confirmObserver.OnTargetStatusChanged -= OnConfirmStatusChanged;
            }
        }

        private void Update()
        {
            if (!boardTracked)
            {
                lockedCell = new Vector2Int(-1, -1);
                return;
            }

            if (aimTracked)
            {
                Vector2Int cell = GetCellFromObserver(aimObserver);

                if (cell.x >= 0 && cell.y >= 0)
                {
                    lockedCell = cell;
                }
                else
                {
                    lockedCell = new Vector2Int(-1, -1);
                }
            }

            if (confirmJustDetected)
            {
                confirmJustDetected = false;
                TryConfirmMultiplayerShot();
            }
        }

        private void TryConfirmMultiplayerShot()
        {
            if (lockedCell.x < 0 || lockedCell.y < 0)
            {
                return;
            }

            if (multiplayerSession == null)
            {
                Debug.LogWarning("[Multiplayer AR] Multiplayer session is missing.");
                return;
            }

            multiplayerSession.FireShot(lockedCell.x, lockedCell.y);

            Debug.Log(
                $"[Multiplayer AR] Requested shot at ({lockedCell.x}, {lockedCell.y})."
            );
        }

        private void OnBoardStatusChanged(
            ObserverBehaviour observer,
            TargetStatus status)
        {
            boardTracked = status.Status == Status.TRACKED
                || status.Status == Status.EXTENDED_TRACKED;

            if (!boardTracked)
            {
                lockedCell = new Vector2Int(-1, -1);
                confirmJustDetected = false;
            }
        }

        private void OnAimStatusChanged(
            ObserverBehaviour observer,
            TargetStatus status)
        {
            aimTracked = status.Status == Status.TRACKED;

            if (!aimTracked)
            {
                smoothingInitialized = false;
            }
        }

        private void OnConfirmStatusChanged(
            ObserverBehaviour observer,
            TargetStatus status)
        {
            bool wasTracked = confirmTracked;
            confirmTracked = status.Status == Status.TRACKED;

            if (!wasTracked && confirmTracked)
            {
                confirmJustDetected = true;
            }
        }

        private Vector2Int GetCellFromObserver(ObserverBehaviour observer)
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
                smoothedLocalPos = Vector3.Lerp(
                    smoothedLocalPos,
                    localPos,
                    Time.deltaTime * smoothSpeed
                );
            }

            localPos = smoothedLocalPos;

            float halfW = boardWidth / 2f;
            float halfH = boardHeight / 2f;

            float gridLocalX = localPos.x - offsetX;
            float gridLocalZ = localPos.z - offsetZ;

            int col = (gridCols - 1)
                - Mathf.FloorToInt((gridLocalX + halfW) / cellSize);

            int row = (gridRows - 1)
                - Mathf.FloorToInt((gridLocalZ + halfH) / cellSize);

            bool valid = col >= 0
                && col < gridCols
                && row >= 0
                && row < gridRows
                && Mathf.Abs(localPos.y) < 5.0f;

            return valid
                ? new Vector2Int(col, row)
                : new Vector2Int(-1, -1);
        }
    }
}