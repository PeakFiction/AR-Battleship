using System.Collections.Generic;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Domain;
using ARBattleship.Multiplayer.Battleship;
using UnityEngine;
using UnityEngine.UI;

namespace ARBattleship.Unity.UI
{
    public sealed class MultiplayerPlacementUI : MonoBehaviour
    {
        [SerializeField] private MultiplayerBattleshipSession session;

        private readonly HashSet<string> placedShips = new HashSet<string>();

        private string selectedShip = "Carrier";
        private Orientation currentOrientation = Orientation.Horizontal;

        private Button[] shipButtons = new Button[5];
        private Button orientationButton;
        private Button[][] gridButtons = new Button[10][];
        private Text orientationLabel;
        private GameObject panel;

        private int localPlayerNumber;

        private readonly string[] shipTypes =
        {
            "Carrier",
            "Battleship",
            "Cruiser",
            "Submarine",
            "Destroyer"
        };

        private void Start()
        {
            BuildUI();
        }

        private void OnEnable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned += OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted += OnShipPlacementAccepted;
            NetworkBattleshipEvents.ShipPlacementRejected += OnShipPlacementRejected;
            NetworkBattleshipEvents.BattleStarted += OnBattleStarted;
        }

        private void OnDisable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned -= OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted -= OnShipPlacementAccepted;
            NetworkBattleshipEvents.ShipPlacementRejected -= OnShipPlacementRejected;
            NetworkBattleshipEvents.BattleStarted -= OnBattleStarted;
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            panel = new GameObject("PlacementPanel");
            panel.transform.SetParent(canvas.transform, false);
            panel.transform.SetAsLastSibling();

            RectTransform panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.6f);

            for (int i = 0; i < shipTypes.Length; i++)
            {
                int index = i;

                GameObject btn = CreateButton(
                    panel.transform,
                    shipTypes[i],
                    new Vector2(-300, 150 - i * 60),
                    new Vector2(200, 50)
                );

                shipButtons[i] = btn.GetComponent<Button>();
                shipButtons[i].onClick.AddListener(
                    () => SelectShip(shipTypes[index])
                );
            }

            GameObject orientBtn = CreateButton(
                panel.transform,
                "Horizontal",
                new Vector2(-300, -160),
                new Vector2(200, 50)
            );

            orientationButton = orientBtn.GetComponent<Button>();
            orientationLabel = orientBtn.GetComponentInChildren<Text>();
            orientationButton.onClick.AddListener(ToggleOrientation);

            float cellSize = 40f;
            float gridOriginX = 100f;
            float gridOriginY = 200f;

            for (int x = 0; x < 10; x++)
            {
                gridButtons[x] = new Button[10];

                for (int y = 0; y < 10; y++)
                {
                    int cx = x;
                    int cy = y;

                    float px = gridOriginX + x * cellSize - 200f;
                    float py = gridOriginY - y * cellSize - 200f;

                    GameObject cell = CreateButton(
                        panel.transform,
                        "",
                        new Vector2(px, py),
                        new Vector2(cellSize - 2, cellSize - 2)
                    );

                    gridButtons[x][y] = cell.GetComponent<Button>();
                    gridButtons[x][y].onClick.AddListener(
                        () => OnCellClicked(cx, cy)
                    );
                }
            }
        }

        private void SelectShip(string shipType)
        {
            if (placedShips.Contains(shipType))
            {
                Debug.Log($"[MultiplayerPlacementUI] {shipType} already placed.");
                return;
            }

            selectedShip = shipType;
            Debug.Log("Selected: " + shipType);
        }

        private void ToggleOrientation()
        {
            currentOrientation = currentOrientation == Orientation.Horizontal
                ? Orientation.Vertical
                : Orientation.Horizontal;

            orientationLabel.text = currentOrientation.ToString();
            Debug.Log("Orientation: " + currentOrientation);
        }

        private void OnCellClicked(int x, int y)
        {
            Debug.Log(
                $"[MultiplayerPlacementUI] Clicked cell ({x},{y}), " +
                $"selected={selectedShip}, orientation={currentOrientation}"
            );

            if (session == null)
            {
                Debug.LogWarning("[MultiplayerPlacementUI] Session is missing.");
                return;
            }

            if (placedShips.Contains(selectedShip))
            {
                Debug.LogWarning(
                    $"[MultiplayerPlacementUI] {selectedShip} already placed."
                );
                return;
            }

            session.PlaceShip(selectedShip, x, y, currentOrientation);
        }

        private void OnLocalPlayerAssigned(int playerNumber)
        {
            localPlayerNumber = playerNumber;
            Debug.Log($"[MultiplayerPlacementUI] You are Player {playerNumber}.");
        }

        private void OnShipPlacementAccepted(
            int playerNumber,
            string shipType,
            int startX,
            int startY,
            int orientationValue)
        {
            if (playerNumber != localPlayerNumber)
            {
                return;
            }

            Orientation orientation = (Orientation)orientationValue;
            Coordinate startCoordinate = new Coordinate(startX, startY);

            Debug.Log($"Placed {shipType} at ({startX},{startY}) {orientation}");

            ColorGridCells(shipType, startCoordinate, orientation, Color.green);
            placedShips.Add(shipType);
            DisableShipButton(shipType);
            SelectNextUnplacedShip();

            if (placedShips.Count == shipTypes.Length)
            {
                Debug.Log("All your ships placed. Requesting game start...");
                session.StartGame();
            }
        }

        private void OnShipPlacementRejected(
            int playerNumber,
            GameErrorCode errorCode)
        {
            if (playerNumber != localPlayerNumber)
            {
                return;
            }

            Debug.LogWarning(
                $"[MultiplayerPlacementUI] Cannot place {selectedShip}: {errorCode}"
            );
        }

        private void OnBattleStarted(int startingPlayerNumber)
        {
            Debug.Log($"[MultiplayerPlacementUI] Battle started. Player {startingPlayerNumber} goes first.");

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void DisableShipButton(string shipType)
        {
            for (int i = 0; i < shipTypes.Length; i++)
            {
                if (shipTypes[i] == shipType)
                {
                    shipButtons[i].interactable = false;
                    return;
                }
            }
        }

        private void SelectNextUnplacedShip()
        {
            foreach (string shipType in shipTypes)
            {
                if (!placedShips.Contains(shipType))
                {
                    selectedShip = shipType;
                    return;
                }
            }
        }

        private void ColorGridCells(
            string shipType,
            Coordinate start,
            Orientation orientation,
            Color color)
        {
            int size = Ship.GetSize(shipType);
            Coordinate offset = orientation.GetOffset();

            for (int i = 0; i < size; i++)
            {
                int cx = start.X + offset.X * i;
                int cy = start.Y + offset.Y * i;

                if (cx < 0 || cx >= 10 || cy < 0 || cy >= 10)
                {
                    continue;
                }

                gridButtons[cx][cy].GetComponent<Image>().color = color;
            }
        }

        private GameObject CreateButton(
            Transform parent,
            string label,
            Vector2 anchoredPos,
            Vector2 size)
        {
            GameObject obj = new GameObject(label == "" ? "Cell" : label);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image image = obj.AddComponent<Image>();
            image.color = Color.gray;

            Button button = obj.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);

            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 14;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;

            return obj;
        }
    }
}