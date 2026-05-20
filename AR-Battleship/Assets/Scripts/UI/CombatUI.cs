using UnityEngine;
using UnityEngine.UI;
using ARBattleship.Core.Application.Enums;
using ARBattleship.Core.Domain;

namespace ARBattleship.Unity.UI
{
    public class CombatUI : MonoBehaviour
    {
        private GameObject combatPanel;
        private GameObject minimapPanel;
        private GameObject battleLogPanel;
        private Text battleLogText;

        private Button[][] enemyGridButtons = new Button[10][];
        private Image[][] minimapCells = new Image[10][];

        private void OnEnable()
        {
            GameManager.OnGameStarted += HandleGameStarted;
            GameManager.OnPlayerShotFired += HandlePlayerShot;
            GameManager.OnEnemyShotFired += HandleEnemyShot;
            GameManager.OnGameOver += HandleGameOver;
            GameManager.OnBattleLogEntry += HandleBattleLogEntry;
        }

        private void OnDisable()
        {
            GameManager.OnGameStarted -= HandleGameStarted;
            GameManager.OnPlayerShotFired -= HandlePlayerShot;
            GameManager.OnEnemyShotFired -= HandleEnemyShot;
            GameManager.OnGameOver -= HandleGameOver;
            GameManager.OnBattleLogEntry -= HandleBattleLogEntry;
        }

        private void Start()
        {
            BuildCombatUI();
            BuildMinimapUI();
            BuildBattleLog();
            combatPanel.SetActive(false);
            minimapPanel.SetActive(false);
            battleLogPanel.SetActive(false);
        }

        private void HandleGameStarted()
        {
            combatPanel.SetActive(true);
            minimapPanel.SetActive(true);
            battleLogPanel.SetActive(true);
            RefreshMinimap();
        }

        private void HandlePlayerShot(int x, int y, ShotOutcome outcome, int? hitSegmentIndex, string? shipOrientation, string? shipType)
        {
            Color color = outcome == ShotOutcome.Miss ? Color.blue : Color.red;
            enemyGridButtons[x][y].GetComponent<Image>().color = color;
            enemyGridButtons[x][y].interactable = false;
        }

        private void HandleEnemyShot(int x, int y, ShotOutcome outcome, int? hitSegmentIndex, string? shipOrientation, string? shipType)
        {
            Color color = outcome == ShotOutcome.Miss ? Color.blue : Color.red;
            minimapCells[x][y].color = color;
        }

        private void HandleGameOver(int winnerIndex)
        {
            combatPanel.SetActive(false);
        }

        private void HandleBattleLogEntry(string entry)
        {
            battleLogText.text = entry + "\n" + battleLogText.text;
        }

        private void OnCellClicked(int x, int y)
        {
            if (GameManager.Instance.CurrentPhase != GamePhase.InProgress) return;
            if (GameManager.Instance.CurrentPlayerTurn != 0) return;

            ShotOutcome outcome = GameManager.Instance.FireShot(x, y);
            Debug.Log($"Fired at ({x},{y}): {outcome}");
        }

        private void RefreshMinimap()
        {
            // Replaced PlacementValidator.GetOccupiedCells with GameSnapshot
            var snapshot = GameManager.Instance.GetSnapshot();
            foreach (var cell in snapshot.PlayerOne.Cells)
            {
                if (cell.State == CellViewState.Ship)
                    minimapCells[cell.X][cell.Y].color = Color.green;
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
                    int cx = x, cy = y;
                    float px = startX + x * cellSize;
                    float py = startY - y * cellSize;
                    GameObject cell = CreateButton(combatPanel.transform, "", new Vector2(px, py), new Vector2(cellSize - 2, cellSize - 2));
                    enemyGridButtons[x][y] = cell.GetComponent<Button>();
                    enemyGridButtons[x][y].onClick.AddListener(() => OnCellClicked(cx, cy));
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
        }

        private GameObject CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = new GameObject(label == "" ? "Cell" : label);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            obj.AddComponent<Image>().color = Color.grey;
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
            text.fontSize = 12;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return obj;
        }
    }
}