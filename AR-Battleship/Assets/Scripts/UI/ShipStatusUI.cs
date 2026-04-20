using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShipStatusUI : MonoBehaviour
{
    private GameObject statusPanel;

    private Dictionary<ShipType, Image> playerShipIndicators = new Dictionary<ShipType, Image>();
    private Dictionary<ShipType, Image> enemyShipIndicators = new Dictionary<ShipType, Image>();
    private Dictionary<ShipType, Text> enemyShipLabels = new Dictionary<ShipType, Text>();

    private readonly ShipType[] shipTypes = new ShipType[]
    {
        ShipType.AircraftCarrier,
        ShipType.Battleship,
        ShipType.Cruiser,
        ShipType.Submarine,
        ShipType.Destroyer
    };

    private void OnEnable()
    {
        GameManager.OnGameStarted += HandleGameStarted;
        GameManager.OnShipSunk += HandleShipSunk;
    }

    private void OnDisable()
    {
        GameManager.OnGameStarted -= HandleGameStarted;
        GameManager.OnShipSunk -= HandleShipSunk;
    }

    private void Start()
    {
        BuildStatusUI();
        statusPanel.SetActive(false);
    }

    private void HandleGameStarted()
    {
        statusPanel.SetActive(true);
    }

    private void HandleShipSunk(int playerIndex, ShipType shipType)
    {
        if (playerIndex == 0)
        {
            // Player's ship sunk — grey it out
            if (playerShipIndicators.ContainsKey(shipType))
                playerShipIndicators[shipType].color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }
        else
        {
            // Enemy's ship sunk — reveal name and grey out
            if (enemyShipIndicators.ContainsKey(shipType))
                enemyShipIndicators[shipType].color = new Color(0.3f, 0.3f, 0.3f, 1f);
            if (enemyShipLabels.ContainsKey(shipType))
            {
                enemyShipLabels[shipType].text = GetShipLabel(shipType) + " [SUNK]";
                enemyShipLabels[shipType].color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }
    }

    private void BuildStatusUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        statusPanel = new GameObject("ShipStatusPanel");
        statusPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRT = statusPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 1f);
        panelRT.anchorMax = new Vector2(0f, 1f);
        panelRT.pivot = new Vector2(0f, 1f);
        panelRT.anchoredPosition = new Vector2(10f, -10f);
        panelRT.sizeDelta = new Vector2(260f, 320f);

        statusPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // --- Player Fleet Header ---
        BuildLabel(statusPanel.transform, "YOUR FLEET", new Vector2(10f, -10f), new Vector2(240f, 25f), Color.white, 14, FontStyle.Bold);

        for (int i = 0; i < shipTypes.Length; i++)
        {
            ShipType type = shipTypes[i];
            float yOffset = -40f - i * 28f;

            // Colour indicator dot
            GameObject dot = new GameObject("PlayerDot_" + type);
            dot.transform.SetParent(statusPanel.transform, false);
            RectTransform dotRT = dot.AddComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0f, 1f);
            dotRT.anchorMax = new Vector2(0f, 1f);
            dotRT.pivot = new Vector2(0f, 1f);
            dotRT.anchoredPosition = new Vector2(10f, yOffset);
            dotRT.sizeDelta = new Vector2(14f, 14f);
            Image dotImg = dot.AddComponent<Image>();
            dotImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            playerShipIndicators[type] = dotImg;

            // Label
            BuildLabel(statusPanel.transform, GetShipLabel(type), new Vector2(32f, yOffset + 2f), new Vector2(200f, 18f), Color.white, 12, FontStyle.Normal);
        }

        // --- Enemy Fleet Header ---
        float enemyHeaderY = -40f - shipTypes.Length * 28f - 20f;
        BuildLabel(statusPanel.transform, "ENEMY FLEET", new Vector2(10f, enemyHeaderY), new Vector2(240f, 25f), new Color(0.85f, 0.1f, 0.1f), 14, FontStyle.Bold);

        for (int i = 0; i < shipTypes.Length; i++)
        {
            ShipType type = shipTypes[i];
            float yOffset = enemyHeaderY - 28f - i * 28f;

            // Colour indicator dot
            GameObject dot = new GameObject("EnemyDot_" + type);
            dot.transform.SetParent(statusPanel.transform, false);
            RectTransform dotRT = dot.AddComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0f, 1f);
            dotRT.anchorMax = new Vector2(0f, 1f);
            dotRT.pivot = new Vector2(0f, 1f);
            dotRT.anchoredPosition = new Vector2(10f, yOffset);
            dotRT.sizeDelta = new Vector2(14f, 14f);
            Image dotImg = dot.AddComponent<Image>();
            dotImg.color = new Color(0.85f, 0.1f, 0.1f, 1f);
            enemyShipIndicators[type] = dotImg;

            // Label — hidden until sunk
            Text label = BuildLabel(statusPanel.transform, "???", new Vector2(32f, yOffset + 2f), new Vector2(200f, 18f), new Color(0.6f, 0.6f, 0.6f), 12, FontStyle.Normal);
            enemyShipLabels[type] = label;
        }
    }

    private Text BuildLabel(Transform parent, string text, Vector2 pos, Vector2 size, Color color, int fontSize, FontStyle style)
    {
        GameObject obj = new GameObject("Label_" + text);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleLeft;

        return t;
    }

    private string GetShipLabel(ShipType type)
    {
        return type switch
        {
            ShipType.AircraftCarrier => "Aircraft Carrier (5)",
            ShipType.Battleship      => "Battleship (4)",
            ShipType.Cruiser         => "Cruiser (3)",
            ShipType.Submarine       => "Submarine (3)",
            ShipType.Destroyer       => "Destroyer (2)",
            _                        => type.ToString()
        };
    }
}