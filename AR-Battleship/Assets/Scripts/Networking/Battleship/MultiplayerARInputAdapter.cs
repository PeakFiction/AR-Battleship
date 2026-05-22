// Converts Vuforia target tracking into multiplayer Battleship shot requests.
// The board target defines the grid space, the aim target selects a cell,
// and the confirm target submits the currently locked cell.

using UnityEngine;
using Vuforia;

namespace ARBattleship.Multiplayer.Battleship
{
    /// <summary>
    /// Bridges AR marker tracking and the multiplayer session by turning tracked
    /// marker positions into Battleship grid coordinates.
    /// </summary>
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

        // (-1, -1) means no valid cell is currently selected.
        private Vector2Int lockedCell = new Vector2Int(-1, -1);

        private void Start()
        {
            // Cache the physical grid dimensions once so coordinate conversion
            // does not recalculate them every frame.
            boardWidth = gridCols * cellSize;
            boardHeight = gridRows * cellSize;

            // Subscribe to Vuforia tracking status changes for all markers.
            boardObserver.OnTargetStatusChanged += OnBoardStatusChanged;
            aimObserver.OnTargetStatusChanged += OnAimStatusChanged;
            confirmObserver.OnTargetStatusChanged += OnConfirmStatusChanged;
        }

        private void OnDestroy()
        {
            // Always unsubscribe from Vuforia events to prevent callbacks from
            // firing on destroyed scene objects.
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
            // A selected cell only makes sense when the board target is visible.
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

            // Submit the shot only on the transition from untracked to tracked,
            // rather than every frame while the confirm marker stays visible.
            if (confirmJustDetected)
            {
                confirmJustDetected = false;
                TryConfirmMultiplayerShot();
            }
        }

        /// <summary>
        /// Sends the currently locked cell to the multiplayer session if the
        /// selected cell and session reference are valid.
        /// </summary>
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
            // Extended tracking is accepted for the board because the board can
            // remain stable even when the target is partially out of view.
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

            // Treat the first detected frame as a button press.
            if (!wasTracked && confirmTracked)
            {
                confirmJustDetected = true;
            }
        }

        /// <summary>
        /// Converts a Vuforia observer position into a Battleship grid coordinate.
        /// Returns (-1, -1) when the marker is outside the configured board area.
        /// </summary>
        private Vector2Int GetCellFromObserver(ObserverBehaviour observer)
        {
            // Convert the marker world position into the board target's local space.
            Vector3 localPos = boardObserver.transform.InverseTransformPoint(
                observer.transform.position
            );

            // Smooth the marker position to reduce cell flicker caused by small
            // tracking jitter between frames.
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

            // Apply Inspector offsets so the logical grid can be aligned with
            // the printed/tracked image.
            float gridLocalX = localPos.x - offsetX;
            float gridLocalZ = localPos.z - offsetZ;

            int col = (gridCols - 1)
                - Mathf.FloorToInt((gridLocalX + halfW) / cellSize);

            int row = (gridRows - 1)
                - Mathf.FloorToInt((gridLocalZ + halfH) / cellSize);

            // The Y threshold rejects markers that are not close enough to the
            // board plane to be considered intentional board input.
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
