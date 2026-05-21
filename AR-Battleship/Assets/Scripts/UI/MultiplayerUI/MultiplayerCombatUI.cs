using System.Collections.Generic;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Domain;
using ARBattleship.Multiplayer.Battleship;
using UnityEngine;
using UnityEngine.UI;

namespace ARBattleship.Unity.UI
{
    public sealed class MultiplayerCombatUI : MonoBehaviour
    {
        [SerializeField] private MultiplayerBattleshipSession session;

        private readonly HashSet<string> requestedShots = new HashSet<string>();
        private readonly HashSet<string> resolvedShots = new HashSet<string>();

        private GameObject combatPanel;
        private GameObject minimapPanel;
        private GameObject battleLogPanel;
        private Text battleLogText;

        private bool battleStarted = false;
        private bool gridVisible = false;
        private bool isBuilt = false;

        private Button[][] enemyGridButtons = new Button[10][];
        private Image[][] minimapCells = new Image[10][];

        private int localPlayerNumber;

        private void Awake()
        {
            BuildIfNeeded();
            HideAllPanels();
        }

        private void OnEnable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned += OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted += OnShipPlacementAccepted;
            NetworkBattleshipEvents.BattleStarted += OnBattleStarted;
            NetworkBattleshipEvents.ShotResolved += OnShotResolved;
            NetworkBattleshipEvents.ShotRejected += OnShotRejected;
        }

        private void OnDisable()
        {
            NetworkBattleshipEvents.LocalPlayerAssigned -= OnLocalPlayerAssigned;
            NetworkBattleshipEvents.ShipPlacementAccepted -= OnShipPlacementAccepted;
            NetworkBattleshipEvents.BattleStarted -= OnBattleStarted;
            NetworkBattleshipEvents.ShotResolved -= OnShotResolved;
            NetworkBattleshipEvents.ShotRejected -= OnShotRejected;
        }

        public void SetGridVisible(bool visible)
        {
            gridVisible = visible;

            if (combatPanel != null)
            {
                combatPanel.SetActive(battleStarted && visible);
            }
        }

        public void ToggleGridVisible()
        {
            SetGridVisible(!gridVisible);
        }

        public bool IsGridVisible()
        {
            return gridVisible;
        }

        private void BuildIfNeeded()
        {
            if (isBuilt)
            {
                return;
            }

            BuildCombatUI();
            BuildMinimapUI();
            BuildBattleLog();

            isBuilt = true;
        }

        private void HideAllPanels()
        {
            battleStarted = false;
            gridVisible = false;

            if (combatPanel != null)
            {
                combatPanel.SetActive(false);
            }

            if (minimapPanel != null)
            {
                minimapPanel.SetActive(false);
            }

            if (battleLogPanel != null)
            {
                battleLogPanel.SetActive(false);
            }
        }

        private void OnLocalPlayerAssigned(int playerNumber)
        {
            localPlayerNumber = playerNumber;
            AddBattleLog($"You are Player {playerNumber}.");
        }

        private void OnBattleStarted(int startingPlayerNumber)
        {
            battleStarted = true;

            SetGridVisible(true);

            if (minimapPanel != null)
            {
                minimapPanel.SetActive(true);
            }

            if (battleLogPanel != null)
            {
                battleLogPanel.SetActive(true);
            }

            AddBattleLog($"Battle started. Player {startingPlayerNumber} goes first.");
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

            ColorShipOnMinimap(
                shipType,
                startCoordinate,
                orientation,
                Color.green
            );

            AddBattleLog($"Your {shipType} was placed on the minimap.");
        }

        private void OnShotResolved(
            int shooterPlayerNumber,
            int x,
            int y,
            ShotOutcome outcome,
            int? hitSegmentIndex,
            string shipOrientation,
            string shipType)
        {
            bool localPlayerFired = shooterPlayerNumber == localPlayerNumber;

            if (localPlayerFired)
            {
                string key = MakeKey(x, y);

                requestedShots.Remove(key);
                resolvedShots.Add(key);

                Color color = outcome == ShotOutcome.Miss ? Color.blue : Color.red;

                if (outcome == ShotOutcome.Sunk)
                {
                    color = Color.black;
                }

                enemyGridButtons[x][y].GetComponent<Image>().color = color;
                enemyGridButtons[x][y].interactable = false;

                AddBattleLog($"You fired at ({x},{y}): {outcome}");
                return;
            }

            Color enemyColor = outcome == ShotOutcome.Miss ? Color.blue : Color.red;

            if (outcome == ShotOutcome.Sunk)
            {
                enemyColor = Color.black;
            }

            minimapCells[x][y].color = enemyColor;
            AddBattleLog($"Opponent fired at ({x},{y}): {outcome}");
        }

        private void OnShotRejected(
            int shooterPlayerNumber,
            int x,
            int y,
            GameErrorCode errorCode)
        {
            if (shooterPlayerNumber != localPlayerNumber)
            {
                return;
            }

            requestedShots.Remove(MakeKey(x, y));
            AddBattleLog($"Shot rejected at ({x},{y}): {errorCode}");
        }

        private void OnCellClicked(int x, int y)
        {
            if (!battleStarted)
            {
                AddBattleLog("Battle has not started yet.");
                return;
            }

            if (session == null)
            {
                AddBattleLog("Multiplayer session is missing.");
                return;
            }

            string key = MakeKey(x, y);

            if (resolvedShots.Contains(key))
            {
                AddBattleLog($"Already fired at ({x},{y}).");
                return;
            }

            if (requestedShots.Contains(key))
            {
                AddBattleLog($"Shot at ({x},{y}) is already pending.");
                return;
            }

            requestedShots.Add(key);
            AddBattleLog($"Requested shot at ({x},{y}).");

            session.FireShot(x, y);
        }

        private void ColorShipOnMinimap(
            string shipType,
            Coordinate start,
            Orientation orientation,
            Color color)
        {
            if (minimapCells == null)
            {
                return;
            }

            int size = Ship.GetSize(shipType);
            Coordinate offset = orientation.GetOffset();

            for (int i = 0; i < size; i++)
            {
                int x = start.X + offset.X * i;
                int y = start.Y + offset.Y * i;

                if (x < 0 || x >= 10 || y < 0 || y >= 10)
                {
                    continue;
                }

                if (minimapCells[x] == null || minimapCells[x][y] == null)
                {
                    continue;
                }

                minimapCells[x][y].color = color;
            }
        }

        private void BuildCombatUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            combatPanel = new GameObject("CombatPanel");
            combatPanel.transform.SetParent(canvas.transform, false);

            RectTransform rt = combatPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            float cellSize = 50f;
            float startX = -225f;
            float startY = 225f;

            for (int x = 0; x < 10; x++)
            {
                enemyGridButtons[x] = new Button[10];

                for (int y = 0; y < 10; y++)
                {
                    int cx = x;
                    int cy = y;

                    float px = startX + x * cellSize;
                    float py = startY - y * cellSize;

                    GameObject cell = CreateButton(
                        combatPanel.transform,
                        "",
                        new Vector2(px, py),
                        new Vector2(cellSize - 2, cellSize - 2)
                    );

                    enemyGridButtons[x][y] = cell.GetComponent<Button>();
                    enemyGridButtons[x][y].onClick.AddListener(
                        () => OnCellClicked(cx, cy)
                    );
                }
            }
        }

        private void BuildMinimapUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            minimapPanel = new GameObject("MinimapPanel");
            minimapPanel.transform.SetParent(canvas.transform, false);

            RectTransform rt = minimapPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(10, 10);
            rt.sizeDelta = new Vector2(210, 210);

            minimapPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            float cellSize = 20f;

            for (int x = 0; x < 10; x++)
            {
                minimapCells[x] = new Image[10];

                for (int y = 0; y < 10; y++)
                {
                    float px = 5f + x * cellSize;
                    float py = 5f + (9 - y) * cellSize;

                    GameObject cell = new GameObject($"Mini_{x}_{y}");
                    cell.transform.SetParent(minimapPanel.transform, false);

                    RectTransform cellRT = cell.AddComponent<RectTransform>();
                    cellRT.anchorMin = Vector2.zero;
                    cellRT.anchorMax = Vector2.zero;
                    cellRT.pivot = Vector2.zero;
                    cellRT.anchoredPosition = new Vector2(px, py);
                    cellRT.sizeDelta = new Vector2(cellSize - 2, cellSize - 2);

                    minimapCells[x][y] = cell.AddComponent<Image>();
                    minimapCells[x][y].color = Color.grey;
                }
            }
        }

        private void BuildBattleLog()
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            battleLogPanel = new GameObject("BattleLogPanel");
            battleLogPanel.transform.SetParent(canvas.transform, false);

            RectTransform rt = battleLogPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-10, 10);
            rt.sizeDelta = new Vector2(250, 300);

            battleLogPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            GameObject textObj = new GameObject("BattleLogText");
            textObj.transform.SetParent(battleLogPanel.transform, false);

            RectTransform textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(5, 5);
            textRT.offsetMax = new Vector2(-5, -5);

            battleLogText = textObj.AddComponent<Text>();
            battleLogText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            battleLogText.fontSize = 12;
            battleLogText.color = Color.white;
            battleLogText.alignment = TextAnchor.UpperLeft;
            battleLogText.text = "";
            battleLogText.raycastTarget = false;
        }

        private void AddBattleLog(string entry)
        {
            if (battleLogText == null)
            {
                Debug.Log($"[MultiplayerCombatUI] {entry}");
                return;
            }

            battleLogText.text = entry + "\n" + battleLogText.text;
        }

        private string MakeKey(int x, int y)
        {
            return x + "," + y;
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
            image.color = Color.grey;

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
            text.fontSize = 12;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;

            return obj;
        }
    }
}