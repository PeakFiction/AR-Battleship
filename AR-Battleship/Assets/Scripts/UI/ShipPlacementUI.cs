using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ARBattleship.Core.Domain;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Multiplayer.Battleship;

namespace ARBattleship.Unity.UI
{
    public class ShipPlacementUI : MonoBehaviour
    {
        private HashSet<string> placedShips = new HashSet<string>();
        private string selectedShip = "Carrier";
        private Orientation currentOrientation = Orientation.Horizontal;

        private Button[] shipButtons = new Button[5];
        private Button orientationButton;
        private Button[][] gridButtons = new Button[10][];
        private Text orientationLabel;
        private GameObject panel;

        private NetworkBattleshipGameController networkController;
        private bool isMultiplayer;
        private int localPlayerNumber = -1;

        private string[] shipTypes = new string[]
        {
            "Carrier",
            "Battleship",
            "Cruiser",
            "Submarine",
            "Destroyer"
        };

        private void Start()
        {
            networkController = FindObjectOfType<NetworkBattleshipGameController>();
            isMultiplayer = networkController != null;

            if (!isMultiplayer)
                GameManager.Instance.ResetGame();

            BuildUI();
        }

        private void OnEnable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned += OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted += OnPlacementAccepted;
            NetworkBattleshipEvents.ShipPlacementRejected += OnPlacementRejected;
            NetworkBattleshipEvents.BattleStarted += OnBattleStarted;
        }

        private void OnDisable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned -= OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted -= OnPlacementAccepted;
            NetworkBattleshipEvents.ShipPlacementRejected -= OnPlacementRejected;
            NetworkBattleshipEvents.BattleStarted -= OnBattleStarted;
        }

        private void OnLocalPlayerAssigned(int playerNumber)
        {
            localPlayerNumber = playerNumber;
        }

        private void OnPlacementAccepted(int playerNumber, string shipType, int startX, int startY, int orientationValue)
        {
            if (playerNumber != localPlayerNumber) return;

            Orientation orient = (Orientation)orientationValue;
            ColorGridCells(shipType, new Coordinate(startX, startY), orient, Color.green);
            placedShips.Add(shipType);

            for (int i = 0; i < shipTypes.Length; i++)
            {
                if (shipTypes[i] == shipType)
                {
                    shipButtons[i].interactable = false;
                    break;
                }
            }

            if (placedShips.Count == shipTypes.Length)
            {
                networkController.RequestStartGame();
                Debug.Log("All ships placed. Waiting for opponent...");
            }
        }

        private void OnPlacementRejected(int playerNumber, GameErrorCode errorCode)
        {
            if (playerNumber != localPlayerNumber) return;
            Debug.Log($"Ship placement rejected: {errorCode}");
        }

        private void OnBattleStarted(int startingPlayerNumber)
        {
            if (panel != null)
                panel.SetActive(false);
            Debug.Log("Battle started!");
        }

        private void BuildUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            panel = new GameObject("PlacementPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            for (int i = 0; i < shipTypes.Length; i++)
            {
                int index = i;
                GameObject btn = CreateButton(panel.transform, shipTypes[i], new Vector2(-300, 150 - i * 60), new Vector2(200, 50));
                shipButtons[i] = btn.GetComponent<Button>();
                shipButtons[i].onClick.AddListener(() => SelectShip(shipTypes[index]));
            }

            GameObject orientBtn = CreateButton(panel.transform, "Horizontal", new Vector2(-300, -160), new Vector2(200, 50));
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
                    int cx = x, cy = y;
                    float px = gridOriginX + x * cellSize - 200f;
                    float py = gridOriginY - y * cellSize - 200f;
                    GameObject cell = CreateButton(panel.transform, "", new Vector2(px, py), new Vector2(cellSize - 2, cellSize - 2));
                    gridButtons[x][y] = cell.GetComponent<Button>();
                    gridButtons[x][y].onClick.AddListener(() => OnCellClicked(cx, cy));
                }
            }
        }

        private void SelectShip(string shipType)
        {
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
            if (isMultiplayer)
            {
                networkController.RequestPlaceShip(selectedShip, x, y, currentOrientation);
            }
            else
            {
                var startCoordinate = new Coordinate(x, y);
                bool placed = GameManager.Instance.PlaceShip(selectedShip, startCoordinate, currentOrientation);

                if (placed)
                {
                    Debug.Log($"Placed {selectedShip} at ({x},{y}) {currentOrientation}");
                    ColorGridCells(selectedShip, startCoordinate, currentOrientation, Color.green);
                    placedShips.Add(selectedShip);

                    for (int i = 0; i < shipTypes.Length; i++)
                    {
                        if (shipTypes[i] == selectedShip)
                        {
                            shipButtons[i].interactable = false;
                            break;
                        }
                    }

                    if (placedShips.Count == shipTypes.Length)
                    {
                        GameManager.Instance.StartGame();
                        panel.SetActive(false);
                        Debug.Log("All ships placed. Game started.");
                    }
                }
                else
                {
                    Debug.Log($"Cannot place {selectedShip} at ({x},{y}) — invalid.");
                }
            }
        }

        private void ColorGridCells(string shipType, Coordinate start, Orientation orientation, Color color)
        {
            int size = ARBattleship.Core.Domain.Ship.GetSize(shipType);
            var offset = orientation.GetOffset();

            for (int i = 0; i < size; i++)
            {
                int cx = start.X + offset.X * i;
                int cy = start.Y + offset.Y * i;
                if (cx < 10 && cy < 10)
                    gridButtons[cx][cy].GetComponent<Image>().color = color;
            }
        }

        private GameObject CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(label == "" ? "Cell" : label);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            obj.AddComponent<Image>().color = Color.gray;
            obj.AddComponent<Button>();

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

            return obj;
        }
    }
}